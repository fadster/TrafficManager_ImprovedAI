using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrafficManager_ImprovedAI.Redirection;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
    public class CustomCarAI : VehicleAI
    {
        public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            frameData.m_position += frameData.m_velocity * 0.5f;
            frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
            float acceleration = this.m_info.m_acceleration;
            float braking = this.m_info.m_braking;
            float magnitude = frameData.m_velocity.magnitude;
            Vector3 vector = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
            float sqrMagnitude = vector.sqrMagnitude;
            float num = (magnitude + acceleration) * (0.5f + 0.5f * (magnitude + acceleration) / braking) + this.m_info.m_generatedInfo.m_size.z * 0.5f;
            float num2 = Mathf.Max(magnitude + acceleration, 5f);
            if (lodPhysics >= 2 && (ulong)(currentFrameIndex >> 4 & 3u) == (ulong)((long)(vehicleID & 3)))
            {
                num2 *= 2f;
            }
            float num3 = Mathf.Max((num - num2) / 3f, 1f);
            float num4 = num2 * num2;
            float num5 = num3 * num3;
            int i = 0;
            bool flag = false;
            if ((sqrMagnitude < num4 || vehicleData.m_targetPos3.w < 0.01f) && (leaderData.m_flags & (Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped)) == Vehicle.Flags.None)
            {
                if (leaderData.m_path != 0u)
                {
                    base.UpdatePathTargetPositions(vehicleID, ref vehicleData, frameData.m_position, ref i, 4, num4, num5);
                    if ((leaderData.m_flags & Vehicle.Flags.Spawned) == Vehicle.Flags.None)
                    {
                        frameData = vehicleData.m_frame0;
                        return;
                    }
                }
                if ((leaderData.m_flags & Vehicle.Flags.WaitingPath) == Vehicle.Flags.None)
                {
                    while (i < 4)
                    {
                        float minSqrDistance;
                        Vector3 refPos;
                        if (i == 0)
                        {
                            minSqrDistance = num4;
                            refPos = frameData.m_position;
                            flag = true;
                        }
                        else
                        {
                            minSqrDistance = num5;
                            refPos = vehicleData.GetTargetPos(i - 1);
                        }
                        int num6 = i;
                        this.UpdateBuildingTargetPositions(vehicleID, ref vehicleData, refPos, leaderID, ref leaderData, ref i, minSqrDistance);
                        if (i == num6)
                        {
                            break;
                        }
                    }
                    if (i != 0)
                    {
                        Vector4 targetPos = vehicleData.GetTargetPos(i - 1);
                        while (i < 4)
                        {
                            vehicleData.SetTargetPos(i++, targetPos);
                        }
                    }
                }
                vector = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
                sqrMagnitude = vector.sqrMagnitude;
            }
            if (leaderData.m_path != 0u && (leaderData.m_flags & Vehicle.Flags.WaitingPath) == Vehicle.Flags.None)
            {
                NetManager instance = Singleton<NetManager>.instance;
                byte b = leaderData.m_pathPositionIndex;
                byte lastPathOffset = leaderData.m_lastPathOffset;
                if (b == 255)
                {
                    b = 0;
                }
                float num7 = 1f + leaderData.CalculateTotalLength(leaderID);
                PathManager instance2 = Singleton<PathManager>.instance;
                PathUnit.Position pathPos;
                if (instance2.m_pathUnits.m_buffer[(int)((UIntPtr)leaderData.m_path)].GetPosition(b >> 1, out pathPos))
                {
                    instance.m_segments.m_buffer[(int)pathPos.m_segment].AddTraffic(Mathf.RoundToInt(num7 * 2.5f));
                    bool flag2 = false;
                    if ((b & 1) == 0 || lastPathOffset == 0)
                    {
                        uint laneID = PathManager.GetLaneID(pathPos);
                        if (laneID != 0u)
                        {
                            Vector3 b2 = instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CalculatePosition((float)pathPos.m_offset * 0.003921569f);
                            float num8 = 0.5f * magnitude * magnitude / this.m_info.m_braking + this.m_info.m_generatedInfo.m_size.z * 0.5f;
                            if (Vector3.Distance(frameData.m_position, b2) >= num8 - 1f)
                            {
                                instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].ReserveSpace(num7);
                                flag2 = true;
                            }
                        }
                    }
                    if (!flag2 && instance2.m_pathUnits.m_buffer[(int)((UIntPtr)leaderData.m_path)].GetNextPosition(b >> 1, out pathPos))
                    {
                        uint laneID2 = PathManager.GetLaneID(pathPos);
                        if (laneID2 != 0u)
                        {
                            instance.m_lanes.m_buffer[(int)((UIntPtr)laneID2)].ReserveSpace(num7);
                        }
                    }
                }
                /* -------------------- Congestion Changes ------------------------- */
                // Not everything is new. Changes are commented
                if ((ulong)(currentFrameIndex >> 4 & 15u) == (ulong)((long)(leaderID & 15)))
                {
                    bool flag3 = false;
                    uint path = leaderData.m_path;
                    int num9 = b >> 1;
                    int j = 0, count = 0; // the count variable is used to keep track of how many of the next 5 lanes are congested
                    //int j = 0;
                    while (j < CustomPathFind.lookaheadLanes)
                    {
                        bool flag4;
                        if (PathUnit.GetNextPosition(ref path, ref num9, out pathPos, out flag4))
                        {
                            uint laneID3 = PathManager.GetLaneID(pathPos);
                            if (laneID3 != 0 && !instance.m_lanes.m_buffer[(int)((UIntPtr)laneID3)].CheckSpace(num7))
                            {
                                j++;
                                ++count; // this lane is congested so increase count
                                continue;
                            }
                        }
                        if (flag4)
                        {
                            this.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                            // flag it as not congested and set count to -1 so that it is neither congested nor completely clear
                            // this is needed here because, contrary to the default code, it does not leave the cycle below
                            flag3 = true;
                            count = -1;
                            break;
                        }
                        flag3 = true;
                        ++j;
                        // the default code would leave the cycle at this point since it found a non congested lane.
                        // this has been changed so that vehicles detect congestions a few lanes in advance.
                        // I am yet to test the performance impact this particular "feature" has.
                    }

                    // if at least 2 out of the next 5 lanes are congested and it hasn't tried to find a new path yet, then calculates a new path and flags it as such
                    // the amounf of congested lanes necessary to calculate a new path can be tweaked to reduce the amount of new paths being calculated, if performance in bigger cities is severely affected
                    if (count >= CustomPathFind.congestedLaneThreshold && (leaderData.m_flags & (Vehicle.Flags)1073741824) == 0)
                    {
                        leaderData.m_flags |= (Vehicle.Flags)1073741824;
                        this.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                    }
                    // if none of the next 5 lanes is congested and the vehicle has already searched for a new path, then it successfully avoided a congestion and the flag is cleared
                    else if (count == 0 && (leaderData.m_flags & (Vehicle.Flags)1073741824) != 0)
                    {
                        leaderData.m_flags &= ~((Vehicle.Flags)1073741824);
                    }
                    // default congestion behavior
                    else if (!flag3)
                        leaderData.m_flags |= Vehicle.Flags.Congestion;
                }
                /* ----------------------------------------------------------------- */
            }
            float num10;
            if ((leaderData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None)
            {
                num10 = 0f;
            }
            else
            {
                num10 = vehicleData.m_targetPos0.w;
            }
            Quaternion rotation = Quaternion.Inverse(frameData.m_rotation);
            vector = rotation * vector;
            Vector3 vector2 = rotation * frameData.m_velocity;
            Vector3 a = Vector3.forward;
            Vector3 vector3 = Vector3.zero;
            Vector3 zero = Vector3.zero;
            float num11 = 0f;
            float num12 = 0f;
            bool flag5 = false;
            float num13 = 0f;
            if (sqrMagnitude > 1f)
            {
                a = VectorUtils.NormalizeXZ(vector, out num13);
                if (num13 > 1f)
                {
                    Vector3 vector4 = vector;
                    num2 = Mathf.Max(magnitude, 2f);
                    num4 = num2 * num2;
                    if (sqrMagnitude > num4)
                    {
                        vector4 *= num2 / Mathf.Sqrt(sqrMagnitude);
                    }
                    bool flag6 = false;
                    if (vector4.z < Mathf.Abs(vector4.x))
                    {
                        if (vector4.z < 0f)
                        {
                            flag6 = true;
                        }
                        float num14 = Mathf.Abs(vector4.x);
                        if (num14 < 1f)
                        {
                            vector4.x = Mathf.Sign(vector4.x);
                            if (vector4.x == 0f)
                            {
                                vector4.x = 1f;
                            }
                            num14 = 1f;
                        }
                        vector4.z = num14;
                    }
                    float b3;
                    a = VectorUtils.NormalizeXZ(vector4, out b3);
                    num13 = Mathf.Min(num13, b3);
                    float num15 = 1.57079637f * (1f - a.z);
                    if (num13 > 1f)
                    {
                        num15 /= num13;
                    }
                    float num16 = num13;
                    if (vehicleData.m_targetPos0.w < 0.1f)
                    {
                        num10 = this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num15);
                        num10 = Mathf.Min(num10, CustomCarAI.CalculateMaxSpeed(num16, Mathf.Min(vehicleData.m_targetPos0.w, vehicleData.m_targetPos1.w), braking * 0.9f));
                    }
                    else
                    {
                        num10 = Mathf.Min(num10, this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num15));
                        num10 = Mathf.Min(num10, CustomCarAI.CalculateMaxSpeed(num16, vehicleData.m_targetPos1.w, braking * 0.9f));
                    }
                    num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos1 - vehicleData.m_targetPos0);
                    num10 = Mathf.Min(num10, CustomCarAI.CalculateMaxSpeed(num16, vehicleData.m_targetPos2.w, braking * 0.9f));
                    num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos2 - vehicleData.m_targetPos1);
                    num10 = Mathf.Min(num10, CustomCarAI.CalculateMaxSpeed(num16, vehicleData.m_targetPos3.w, braking * 0.9f));
                    num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos3 - vehicleData.m_targetPos2);
                    if (vehicleData.m_targetPos3.w < 0.01f)
                    {
                        num16 = Mathf.Max(0f, num16 - this.m_info.m_generatedInfo.m_size.z * 0.5f);
                    }
                    num10 = Mathf.Min(num10, CustomCarAI.CalculateMaxSpeed(num16, 0f, braking * 0.9f));
                    if (!CustomCarAI.DisableCollisionCheck(leaderID, ref leaderData))
                    {
                        this.CheckOtherVehicles(vehicleID, ref vehicleData, ref frameData, ref num10, ref flag5, ref zero, num, braking * 0.9f, lodPhysics);
                    }
                    if (flag6)
                    {
                        num10 = -num10;
                    }
                    if (num10 < magnitude)
                    {
                        float num17 = Mathf.Max(acceleration, Mathf.Min(braking, magnitude));
                        num11 = Mathf.Max(num10, magnitude - num17);
                    }
                    else
                    {
                        float num18 = Mathf.Max(acceleration, Mathf.Min(braking, -magnitude));
                        num11 = Mathf.Min(num10, magnitude + num18);
                    }
                }
            }
            else if (magnitude < 0.1f && flag && this.ArriveAtDestination(leaderID, ref leaderData))
            {
                leaderData.Unspawn(leaderID);
                if (leaderID == vehicleID)
                {
                    frameData = leaderData.m_frame0;
                }
                return;
            }
            if ((leaderData.m_flags & Vehicle.Flags.Stopped) == Vehicle.Flags.None && num10 < 0.1f)
            {
                flag5 = true;
            }
            if (flag5)
            {
                vehicleData.m_blockCounter = (byte)Mathf.Min((int)(vehicleData.m_blockCounter + 1), 255);
                if ((vehicleData.m_blockCounter == 100 || vehicleData.m_blockCounter == 150) && !LoadingExtension.Instance.despawnEnabled)
                    vehicleData.m_blockCounter++;
            }
            else
            {
                vehicleData.m_blockCounter = 0;
            }
            if (num13 > 1f)
            {
                num12 = Mathf.Asin(a.x) * Mathf.Sign(num11);
                vector3 = a * num11;
            }
            else
            {
                num11 = 0f;
                Vector3 b4 = Vector3.ClampMagnitude(vector * 0.5f - vector2, braking);
                vector3 = vector2 + b4;
            }
            bool flag7 = (currentFrameIndex + (uint)leaderID & 16u) != 0u;
            Vector3 a2 = vector3 - vector2;
            Vector3 vector5 = frameData.m_rotation * vector3;
            frameData.m_velocity = vector5 + zero;
            frameData.m_position += frameData.m_velocity * 0.5f;
            frameData.m_swayVelocity = frameData.m_swayVelocity * (1f - this.m_info.m_dampers) - a2 * (1f - this.m_info.m_springs) - frameData.m_swayPosition * this.m_info.m_springs;
            frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
            frameData.m_steerAngle = num12;
            frameData.m_travelDistance += vector3.z;
            frameData.m_lightIntensity.x = 5f;
            frameData.m_lightIntensity.y = ((a2.z >= -0.1f) ? 0.5f : 5f);
            frameData.m_lightIntensity.z = ((num12 >= -0.1f || !flag7) ? 0f : 5f);
            frameData.m_lightIntensity.w = ((num12 <= 0.1f || !flag7) ? 0f : 5f);
            frameData.m_underground = ((vehicleData.m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None);
            frameData.m_transition = ((vehicleData.m_flags & Vehicle.Flags.Transition) != Vehicle.Flags.None);
            if ((vehicleData.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None && num13 <= 1f && flag)
            {
                Vector3 forward = vehicleData.m_targetPos1 - vehicleData.m_targetPos0;
                if (forward.sqrMagnitude > 0.01f)
                {
                    frameData.m_rotation = Quaternion.LookRotation(forward);
                }
            }
            else if (num11 > 0.1f)
            {
                if (vector5.sqrMagnitude > 0.01f)
                {
                    frameData.m_rotation = Quaternion.LookRotation(vector5);
                }
            }
            else if (num11 < -0.1f && vector5.sqrMagnitude > 0.01f)
            {
                frameData.m_rotation = Quaternion.LookRotation(-vector5);
            }
            base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
        }

		protected override void CalculateSegmentPosition(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position nextPosition,
			PathUnit.Position position, uint laneID, byte offset, PathUnit.Position prevPos, uint prevLaneID,
			byte prevOffset, out Vector3 pos, out Vector3 dir, out float maxSpeed)
		{
			NetManager instance = Singleton<NetManager>.instance;
			instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CalculatePositionAndDirection((float)offset * 0.003921569f, out pos, out dir);
			Vehicle.Frame lastFrameData = vehicleData.GetLastFrameData();
			Vector3 position2 = lastFrameData.m_position;
			Vector3 b = instance.m_lanes.m_buffer[(int)((UIntPtr)prevLaneID)].CalculatePosition((float)prevOffset * 0.003921569f);
			float num = 0.5f * lastFrameData.m_velocity.sqrMagnitude / this.m_info.m_braking + this.m_info.m_generatedInfo.m_size.z * 0.5f;

			if (vehicleData.Info.m_vehicleType == VehicleInfo.VehicleType.Car)
			{
				if (!TrafficPriority.vehicleList.ContainsKey(vehicleID))
				{
					TrafficPriority.vehicleList.Add(vehicleID, new PriorityCar());
				}
			}

			if (Vector3.Distance(position2, b) >= num - 1f)
			{
				Segment3 segment;
				segment.a = pos;
				ushort num2;
				ushort num3;
				if (offset < position.m_offset)
				{
					segment.b = pos + dir.normalized*this.m_info.m_generatedInfo.m_size.z;
					num2 = instance.m_segments.m_buffer[(int) position.m_segment].m_startNode;
					num3 = instance.m_segments.m_buffer[(int) position.m_segment].m_endNode;
				}
				else
				{
					segment.b = pos - dir.normalized*this.m_info.m_generatedInfo.m_size.z;
					num2 = instance.m_segments.m_buffer[(int) position.m_segment].m_endNode;
					num3 = instance.m_segments.m_buffer[(int) position.m_segment].m_startNode;
				}
				ushort num4;
				if (prevOffset == 0)
				{
					num4 = instance.m_segments.m_buffer[(int) prevPos.m_segment].m_startNode;
				}
				else
				{
					num4 = instance.m_segments.m_buffer[(int) prevPos.m_segment].m_endNode;
				}

				if (num2 == num4)
				{
					uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
					uint num5 = (uint)(((int)num4 << 8) / 32768);
					uint num6 = currentFrameIndex - num5 & 255u;

					NetNode.Flags flags = instance.m_nodes.m_buffer[(int) num2].m_flags;
					NetLane.Flags flags2 =
						(NetLane.Flags) instance.m_lanes.m_buffer[(int) ((UIntPtr) prevLaneID)].m_flags;
					bool flag = (flags & NetNode.Flags.TrafficLights) != NetNode.Flags.None;
					bool flag2 = (flags & NetNode.Flags.LevelCrossing) != NetNode.Flags.None;
					bool flag3 = (flags2 & NetLane.Flags.JoinedJunction) != NetLane.Flags.None;
					if ((flags & (NetNode.Flags.Junction | NetNode.Flags.OneWayOut | NetNode.Flags.OneWayIn)) ==
						NetNode.Flags.Junction && instance.m_nodes.m_buffer[(int) num2].CountSegments() != 2)
					{
						float len = vehicleData.CalculateTotalLength(vehicleID) + 2f;
						if (!instance.m_lanes.m_buffer[(int) ((UIntPtr) laneID)].CheckSpace(len))
						{
							bool flag4 = false;
							if (nextPosition.m_segment != 0 &&
								instance.m_lanes.m_buffer[(int) ((UIntPtr) laneID)].m_length < 30f)
							{
								NetNode.Flags flags3 = instance.m_nodes.m_buffer[(int) num3].m_flags;
								if ((flags3 &
									(NetNode.Flags.Junction | NetNode.Flags.OneWayOut | NetNode.Flags.OneWayIn)) !=
									NetNode.Flags.Junction || instance.m_nodes.m_buffer[(int) num3].CountSegments() == 2)
								{
									uint laneID2 = PathManager.GetLaneID(nextPosition);
									if (laneID2 != 0u)
									{
										flag4 = instance.m_lanes.m_buffer[(int) ((UIntPtr) laneID2)].CheckSpace(len);
									}
								}
							}
							if (!flag4)
							{
								maxSpeed = 0f;
								return;
							}
						}
					}

					if (vehicleData.Info.m_vehicleType == VehicleInfo.VehicleType.Car &&
						TrafficPriority.vehicleList.ContainsKey(vehicleID) &&
						TrafficPriority.isPrioritySegment(num2, prevPos.m_segment))
					{
						uint currentFrameIndex2 = Singleton<SimulationManager>.instance.m_currentFrameIndex;
						uint frame = currentFrameIndex2 >> 4;

						var prioritySegment = TrafficPriority.getPrioritySegment(num2, prevPos.m_segment);
						if (TrafficPriority.vehicleList[vehicleID].toNode != num2 ||
							TrafficPriority.vehicleList[vehicleID].fromSegment != prevPos.m_segment)
						{
							if (TrafficPriority.vehicleList[vehicleID].toNode != 0 &&
								TrafficPriority.vehicleList[vehicleID].fromSegment != 0)
							{
								var oldNode = TrafficPriority.vehicleList[vehicleID].toNode;
								var oldSegment = TrafficPriority.vehicleList[vehicleID].fromSegment;

								if (TrafficPriority.isPrioritySegment(oldNode, oldSegment))
								{
									var oldPrioritySegment = TrafficPriority.getPrioritySegment(oldNode, oldSegment);
									TrafficPriority.vehicleList[vehicleID].waitTime = 0;
									TrafficPriority.vehicleList[vehicleID].stopped = false;
									oldPrioritySegment.RemoveCar(vehicleID);
								}
							}

							// prevPos - current segment
							// position - next segment
							TrafficPriority.vehicleList[vehicleID].toNode = num2;
							TrafficPriority.vehicleList[vehicleID].fromSegment = prevPos.m_segment;
							TrafficPriority.vehicleList[vehicleID].toSegment = position.m_segment;
							TrafficPriority.vehicleList[vehicleID].toLaneID = PathManager.GetLaneID(position);
							TrafficPriority.vehicleList[vehicleID].fromLaneID = PathManager.GetLaneID(prevPos);
							TrafficPriority.vehicleList[vehicleID].fromLaneFlags =
								instance.m_lanes.m_buffer[PathManager.GetLaneID(prevPos)].m_flags;
							TrafficPriority.vehicleList[vehicleID].yieldSpeedReduce = UnityEngine.Random.Range(13f,
								18f);

							prioritySegment.AddCar(vehicleID);
						}

						TrafficPriority.vehicleList[vehicleID].lastFrame = frame;
						TrafficPriority.vehicleList[vehicleID].lastSpeed =
							vehicleData.GetLastFrameData().m_velocity.sqrMagnitude;
					}

					if (flag && (!flag3 || flag2))
					{
						var nodeSimulation = CustomRoadAI.GetNodeSimulation(num4);

						NetInfo info = instance.m_nodes.m_buffer[(int) num2].Info;
						RoadBaseAI.TrafficLightState vehicleLightState;
						RoadBaseAI.TrafficLightState pedestrianLightState;
						bool flag5;
						bool pedestrians;

						if (nodeSimulation == null || (nodeSimulation.FlagTimedTrafficLights && !nodeSimulation.TimedTrafficLightsActive))
						{
							RoadBaseAI.GetTrafficLightState(num4,
								ref instance.m_segments.m_buffer[(int) prevPos.m_segment],
								currentFrameIndex - num5, out vehicleLightState, out pedestrianLightState, out flag5,
								out pedestrians);
							if (!flag5 && num6 >= 196u)
							{
								flag5 = true;
								RoadBaseAI.SetTrafficLightState(num4,
									ref instance.m_segments.m_buffer[(int) prevPos.m_segment], currentFrameIndex - num5,
									vehicleLightState, pedestrianLightState, flag5, pedestrians);
							}

							if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == Vehicle.Flags.None ||
								info.m_class.m_service != ItemClass.Service.Road)
							{
								switch (vehicleLightState)
								{
								case RoadBaseAI.TrafficLightState.RedToGreen:
									if (num6 < 60u)
									{
										maxSpeed = 0f;
										return;
									}
									break;
								case RoadBaseAI.TrafficLightState.Red:
									maxSpeed = 0f;
									return;
								case RoadBaseAI.TrafficLightState.GreenToRed:
									if (num6 >= 30u)
									{
										maxSpeed = 0f;
										return;
									}
									break;
								}
							}
						}
						else
						{
							var stopCar = false;

							if (TrafficPriority.isLeftSegment(prevPos.m_segment, position.m_segment, num2))
							{
								vehicleLightState =
									TrafficLightsManual.GetSegmentLight(num4, prevPos.m_segment).GetLightLeft();
							}
							else if (TrafficPriority.isRightSegment(prevPos.m_segment, position.m_segment, num2))
							{
								vehicleLightState =
									TrafficLightsManual.GetSegmentLight(num4, prevPos.m_segment).GetLightRight();
							}
							else
							{
								vehicleLightState =
									TrafficLightsManual.GetSegmentLight(num4, prevPos.m_segment).GetLightMain();
							}

							if (vehicleLightState == RoadBaseAI.TrafficLightState.Green)
							{
								var hasIncomingCars = TrafficPriority.incomingVehicles(vehicleID, num2);

								if (hasIncomingCars)
								{
									stopCar = true;
								}
							}

							if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == Vehicle.Flags.None ||
								info.m_class.m_service != ItemClass.Service.Road)
							{
								switch (vehicleLightState)
								{
								case RoadBaseAI.TrafficLightState.RedToGreen:
									if (num6 < 60u)
									{
										stopCar = true;
									}
									break;
								case RoadBaseAI.TrafficLightState.Red:
									stopCar = true;
									break;
								case RoadBaseAI.TrafficLightState.GreenToRed:
									if (num6 >= 30u)
									{
										stopCar = true;
									}
									break;
								}
							}

							if (stopCar)
							{
								maxSpeed = 0f;
								return;
							}
						}
					}
					else
					{
						if (vehicleData.Info.m_vehicleType == VehicleInfo.VehicleType.Car &&
							TrafficPriority.vehicleList.ContainsKey(vehicleID) &&
							TrafficPriority.isPrioritySegment(num2, prevPos.m_segment))
						{
							uint currentFrameIndex2 = Singleton<SimulationManager>.instance.m_currentFrameIndex;
							uint frame = currentFrameIndex2 >> 4;

							var prioritySegment = TrafficPriority.getPrioritySegment(num2, prevPos.m_segment);

							if (TrafficPriority.vehicleList[vehicleID].carState == PriorityCar.CarState.None)
							{
								TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Enter;
							}

							if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == Vehicle.Flags.None &&
								TrafficPriority.vehicleList[vehicleID].carState != PriorityCar.CarState.Leave)
							{
								if (prioritySegment.type == PrioritySegment.PriorityType.Stop)
								{
									if (TrafficPriority.vehicleList[vehicleID].waitTime < 75)
									{
										TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Stop;

										if (vehicleData.GetLastFrameData().m_velocity.sqrMagnitude < 0.1f ||
											TrafficPriority.vehicleList[vehicleID].stopped)
										{
											TrafficPriority.vehicleList[vehicleID].stopped = true;
											TrafficPriority.vehicleList[vehicleID].waitTime++;

											if (TrafficPriority.vehicleList[vehicleID].waitTime > 2)
											{
												var hasIncomingCars = TrafficPriority.incomingVehicles(vehicleID, num2);

												if (hasIncomingCars)
												{
													maxSpeed = 0f;
													return;
												}
											}
											else
											{
												maxSpeed = 0f;
												return;
											}
										}
										else
										{
											maxSpeed = 0f;
											return;
										}
									}
									else
									{
										TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Leave;
									}
								}
								else if (prioritySegment.type == PrioritySegment.PriorityType.Yield)
								{
									if (TrafficPriority.vehicleList[vehicleID].waitTime < 75)
									{
										TrafficPriority.vehicleList[vehicleID].waitTime++;
										TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Stop;
										maxSpeed = 0f;

										if (vehicleData.GetLastFrameData().m_velocity.sqrMagnitude <
											TrafficPriority.vehicleList[vehicleID].yieldSpeedReduce)
										{
											var hasIncomingCars = TrafficPriority.incomingVehicles(vehicleID, num2);

											if (hasIncomingCars)
											{
												return;
											}
										}
										else
										{
											return;
										}
									}
									else
									{
										TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Leave;
									}
								}
								else if (prioritySegment.type == PrioritySegment.PriorityType.Main)
								{
									TrafficPriority.vehicleList[vehicleID].waitTime++;
									TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Stop;
									maxSpeed = 0f;

									var hasIncomingCars = TrafficPriority.incomingVehicles(vehicleID, num2);

									if (hasIncomingCars)
									{
										TrafficPriority.vehicleList[vehicleID].stopped = true;
										return;
									}
									else
									{
										TrafficPriority.vehicleList[vehicleID].stopped = false;

										NetInfo info3 = instance.m_segments.m_buffer[(int) position.m_segment].Info;
										if (info3.m_lanes != null && info3.m_lanes.Length > (int) position.m_lane)
										{
											maxSpeed =
												this.CalculateTargetSpeed(vehicleID, ref vehicleData,
													info3.m_lanes[(int) position.m_lane].m_speedLimit,
													instance.m_lanes.m_buffer[(int) ((UIntPtr) laneID)].m_curve)*0.8f;
										}
										else
										{
											maxSpeed = this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1f, 0f)*
												0.8f;
										}
										return;
									}
								}
							}
							else
							{
								TrafficPriority.vehicleList[vehicleID].carState = PriorityCar.CarState.Transit;
							}
						}
					}
				}
			}

			NetInfo info2 = instance.m_segments.m_buffer[(int)position.m_segment].Info;
			if (info2.m_lanes != null && info2.m_lanes.Length > (int)position.m_lane)
			{
				var laneSpeedLimit = info2.m_lanes[(int) position.m_lane].m_speedLimit;

				if (TrafficRoadRestrictions.isSegment(position.m_segment))
				{
					var restrictionSegment = TrafficRoadRestrictions.getSegment(position.m_segment);

					if (restrictionSegment.speedLimits[(int) position.m_lane] > 0.1f)
					{
						laneSpeedLimit = restrictionSegment.speedLimits[(int) position.m_lane];
					}
				}

				maxSpeed = this.CalculateTargetSpeed(vehicleID, ref vehicleData, laneSpeedLimit, instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].m_curve);
			}
			else
			{
				maxSpeed = this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1f, 0f);
			}
		}
			
        #region Stock Methods
        private static float CalculateMaxSpeed(float targetDistance, float targetSpeed, float maxBraking)
        {
            float num = 0.5f * maxBraking;
            float num2 = num + targetSpeed;
            return Mathf.Sqrt(Mathf.Max(0f, num2 * num2 + 2f * targetDistance * maxBraking)) - num;
        }

        private static bool DisableCollisionCheck(ushort vehicleID, ref Vehicle vehicleData)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.Arriving) != Vehicle.Flags.None)
            {
                float num = Mathf.Max(Mathf.Abs(vehicleData.m_targetPos3.x), Mathf.Abs(vehicleData.m_targetPos3.z));
                float num2 = 8640f;
                if (num > num2 - 100f)
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckOtherVehicles(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxDistance, float maxBraking, int lodPhysics)
        {
            Vector3 vector = (Vector3)vehicleData.m_targetPos3 - frameData.m_position;
            Vector3 rhs = frameData.m_position + Vector3.ClampMagnitude(vector, maxDistance);
            Vector3 min = Vector3.Min(vehicleData.m_segment.Min(), rhs);
            Vector3 max = Vector3.Max(vehicleData.m_segment.Max(), rhs);
            VehicleManager instance = Singleton<VehicleManager>.instance;
            int num = Mathf.Max((int)((min.x - 10f) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((min.z - 10f) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((max.x + 10f) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((max.z + 10f) / 32f + 270f), 539);
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort num5 = instance.m_vehicleGrid[i * 540 + j];
                    int num6 = 0;
                    while (num5 != 0)
                    {
                        num5 = this.CheckOtherVehicle(vehicleID, ref vehicleData, ref frameData, ref maxSpeed, ref blocked, ref collisionPush, maxBraking, num5, ref instance.m_vehicles.m_buffer[(int)num5], min, max, lodPhysics);
                        if (++num6 > 16384)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            if (lodPhysics == 0)
            {
                CitizenManager instance2 = Singleton<CitizenManager>.instance;
                float num7 = 0f;
                Vector3 vector2 = vehicleData.m_segment.b;
                Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
                for (int k = 0; k < 4; k++)
                {
                    Vector3 vector3 = vehicleData.GetTargetPos(k);
                    Vector3 vector4 = vector3 - vector2;
                    if (Vector3.Dot(lhs, vector4) > 0f)
                    {
                        float magnitude = vector4.magnitude;
                        if (magnitude > 0.01f)
                        {
                            Segment3 segment = new Segment3(vector2, vector3);
                            min = segment.Min();
                            max = segment.Max();
                            int num8 = Mathf.Max((int)((min.x - 3f) / 8f + 1080f), 0);
                            int num9 = Mathf.Max((int)((min.z - 3f) / 8f + 1080f), 0);
                            int num10 = Mathf.Min((int)((max.x + 3f) / 8f + 1080f), 2159);
                            int num11 = Mathf.Min((int)((max.z + 3f) / 8f + 1080f), 2159);
                            for (int l = num9; l <= num11; l++)
                            {
                                for (int m = num8; m <= num10; m++)
                                {
                                    ushort num12 = instance2.m_citizenGrid[l * 2160 + m];
                                    int num13 = 0;
                                    while (num12 != 0)
                                    {
                                        num12 = this.CheckCitizen(vehicleID, ref vehicleData, segment, num7, magnitude, ref maxSpeed, ref blocked, maxBraking, num12, ref instance2.m_instances.m_buffer[(int)num12], min, max);
                                        if (++num13 > 65536)
                                        {
                                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        lhs = vector4;
                        num7 += magnitude;
                        vector2 = vector3;
                    }
                }
            }
        }

        private ushort CheckCitizen(ushort vehicleID, ref Vehicle vehicleData, Segment3 segment, float lastLen, float nextLen, ref float maxSpeed, ref bool blocked, float maxBraking, ushort otherID, ref CitizenInstance otherData, Vector3 min, Vector3 max)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.Transition) == Vehicle.Flags.None && (otherData.m_flags & CitizenInstance.Flags.Transition) == CitizenInstance.Flags.None && (vehicleData.m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None != ((otherData.m_flags & CitizenInstance.Flags.Underground) != CitizenInstance.Flags.None))
            {
                return otherData.m_nextGridInstance;
            }
            CitizenInfo info = otherData.Info;
            CitizenInstance.Frame lastFrameData = otherData.GetLastFrameData();
            Vector3 position = lastFrameData.m_position;
            Vector3 b = lastFrameData.m_position + lastFrameData.m_velocity;
            Segment3 segment2 = new Segment3(position, b);
            Vector3 vector = segment2.Min();
            vector.x -= info.m_radius;
            vector.z -= info.m_radius;
            Vector3 vector2 = segment2.Max();
            vector2.x += info.m_radius;
            vector2.y += info.m_height;
            vector2.z += info.m_radius;
            float num;
            float num2;
            if (min.x < vector2.x + 1f && min.y < vector2.y && min.z < vector2.z + 1f && vector.x < max.x + 1f && vector.y < max.y + 2f && vector.z < max.z + 1f && segment.DistanceSqr(segment2, out num, out num2) < (1f + info.m_radius) * (1f + info.m_radius))
            {
                float num3 = lastLen + nextLen * num;
                if (num3 >= 0.01f)
                {
                    num3 -= 2f;
                    float b2 = Mathf.Max(1f, CustomCarAI.CalculateMaxSpeed(num3, 0f, maxBraking));
                    maxSpeed = Mathf.Min(maxSpeed, b2);
                }
            }
            return otherData.m_nextGridInstance;
        }

        private ushort CheckOtherVehicle(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxBraking, ushort otherID, ref Vehicle otherData, Vector3 min, Vector3 max, int lodPhysics)
        {
            if (otherID != vehicleID && vehicleData.m_leadingVehicle != otherID && vehicleData.m_trailingVehicle != otherID)
            {
                VehicleInfo info = otherData.Info;
                if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
                {
                    return otherData.m_nextGridVehicle;
                }
                if (((vehicleData.m_flags | otherData.m_flags) & Vehicle.Flags.Transition) == Vehicle.Flags.None && (vehicleData.m_flags & Vehicle.Flags.Underground) != (otherData.m_flags & Vehicle.Flags.Underground))
                {
                    return otherData.m_nextGridVehicle;
                }
                Vector3 vector;
                Vector3 vector2;
                if (lodPhysics >= 2)
                {
                    vector = otherData.m_segment.Min();
                    vector2 = otherData.m_segment.Max();
                }
                else
                {
                    vector = Vector3.Min(otherData.m_segment.Min(), otherData.m_targetPos3);
                    vector2 = Vector3.Max(otherData.m_segment.Max(), otherData.m_targetPos3);
                }
                if (min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
                {
                    Vehicle.Frame lastFrameData = otherData.GetLastFrameData();
                    if (lodPhysics < 2)
                    {
                        float num2;
                        float num3;
                        float num = vehicleData.m_segment.DistanceSqr(otherData.m_segment, out num2, out num3);
                        if (num < 4f)
                        {
                            Vector3 a = vehicleData.m_segment.Position(0.5f);
                            Vector3 b = otherData.m_segment.Position(0.5f);
                            Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
                            if (Vector3.Dot(lhs, a - b) < 0f)
                            {
                                collisionPush -= lhs.normalized * (0.1f - num * 0.025f);
                            }
                            else
                            {
                                collisionPush += lhs.normalized * (0.1f - num * 0.025f);
                            }
                            blocked = true;
                        }
                    }
                    float num4 = frameData.m_velocity.magnitude + 0.01f;
                    float num5 = lastFrameData.m_velocity.magnitude;
                    float num6 = num5 * (0.5f + 0.5f * num5 / info.m_braking) + Mathf.Min(1f, num5);
                    num5 += 0.01f;
                    float num7 = 0f;
                    Vector3 vector3 = vehicleData.m_segment.b;
                    Vector3 lhs2 = vehicleData.m_segment.b - vehicleData.m_segment.a;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 vector4 = vehicleData.GetTargetPos(i);
                        Vector3 vector5 = vector4 - vector3;
                        if (Vector3.Dot(lhs2, vector5) > 0f)
                        {
                            float magnitude = vector5.magnitude;
                            Segment3 segment = new Segment3(vector3, vector4);
                            min = segment.Min();
                            max = segment.Max();
                            segment.a.y = segment.a.y * 0.5f;
                            segment.b.y = segment.b.y * 0.5f;
                            if (magnitude > 0.01f && min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
                            {
                                Vector3 a2 = otherData.m_segment.a;
                                a2.y *= 0.5f;
                                float num8;
                                if (segment.DistanceSqr(a2, out num8) < 4f)
                                {
                                    float num9 = Vector3.Dot(lastFrameData.m_velocity, vector5) / magnitude;
                                    float num10 = num7 + magnitude * num8;
                                    if (num10 >= 0.01f)
                                    {
                                        num10 -= num9 + 3f;
                                        float num11 = Mathf.Max(0f, CustomCarAI.CalculateMaxSpeed(num10, num9, maxBraking));
                                        if (num11 < 0.01f)
                                        {
                                            blocked = true;
                                        }
                                        Vector3 rhs = Vector3.Normalize((Vector3)otherData.m_targetPos0 - otherData.m_segment.a);
                                        float num12 = 1.2f - 1f / ((float)vehicleData.m_blockCounter * 0.02f + 0.5f);
                                        if (Vector3.Dot(vector5, rhs) > num12 * magnitude)
                                        {
                                            maxSpeed = Mathf.Min(maxSpeed, num11);
                                        }
                                    }
                                    break;
                                }
                                if (lodPhysics < 2)
                                {
                                    float num13 = 0f;
                                    float num14 = num6;
                                    Vector3 vector6 = otherData.m_segment.b;
                                    Vector3 lhs3 = otherData.m_segment.b - otherData.m_segment.a;
                                    bool flag = false;
                                    int num15 = 0;
                                    while (num15 < 4 && num14 > 0.1f)
                                    {
                                        Vector3 vector7 = otherData.GetTargetPos(num15);
                                        Vector3 vector8 = Vector3.ClampMagnitude(vector7 - vector6, num14);
                                        if (Vector3.Dot(lhs3, vector8) > 0f)
                                        {
                                            vector7 = vector6 + vector8;
                                            float magnitude2 = vector8.magnitude;
                                            num14 -= magnitude2;
                                            Segment3 segment2 = new Segment3(vector6, vector7);
                                            segment2.a.y = segment2.a.y * 0.5f;
                                            segment2.b.y = segment2.b.y * 0.5f;
                                            if (magnitude2 > 0.01f)
                                            {
                                                float num17;
                                                float num18;
                                                float num16;
                                                if (otherID < vehicleID)
                                                {
                                                    num16 = segment2.DistanceSqr(segment, out num17, out num18);
                                                }
                                                else
                                                {
                                                    num16 = segment.DistanceSqr(segment2, out num18, out num17);
                                                }
                                                if (num16 < 4f)
                                                {
                                                    float num19 = num7 + magnitude * num18;
                                                    float num20 = num13 + magnitude2 * num17 + 0.1f;
                                                    if (num19 >= 0.01f && num19 * num5 > num20 * num4)
                                                    {
                                                        float num21 = Vector3.Dot(lastFrameData.m_velocity, vector5) / magnitude;
                                                        if (num19 >= 0.01f)
                                                        {
                                                            num19 -= num21 + 1f + otherData.Info.m_generatedInfo.m_size.z;
                                                            float num22 = Mathf.Max(0f, CustomCarAI.CalculateMaxSpeed(num19, num21, maxBraking));
                                                            if (num22 < 0.01f)
                                                            {
                                                                blocked = true;
                                                            }
                                                            maxSpeed = Mathf.Min(maxSpeed, num22);
                                                        }
                                                    }
                                                    flag = true;
                                                    break;
                                                }
                                            }
                                            lhs3 = vector8;
                                            num13 += magnitude2;
                                            vector6 = vector7;
                                        }
                                        num15++;
                                    }
                                    if (flag)
                                    {
                                        break;
                                    }
                                }
                            }
                            lhs2 = vector5;
                            num7 += magnitude;
                            vector3 = vector4;
                        }
                    }
                }
            }
            return otherData.m_nextGridVehicle;
        }

        #endregion

        public static void RedirectCalls(List<RedirectCallsState> callStates)
        {
            MethodInfo originalMethod = typeof(CarAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 6);
            MethodInfo replacementMethod = typeof(CustomCarAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 6);

            if (originalMethod != null && replacementMethod != null)
            {
                callStates.Add(RedirectionHelper.RedirectCalls(originalMethod, replacementMethod));
            }
        }
    }

    public class CustomPassengerCarAI : CarAI
    {
        public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (!LoadingExtension.Instance.despawnEnabled) {
                data.m_flags &= ~Vehicle.Flags.Congestion;
            }

            if ((data.m_flags & Vehicle.Flags.Congestion) != Vehicle.Flags.None)
            {
                Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            }
            else
            {
                base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
            }
        }

        public static void RedirectCalls(List<RedirectCallsState> callStates)
        {
            MethodInfo originalMethod = typeof(PassengerCarAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 3);
            MethodInfo replacementMethod = typeof(CustomPassengerCarAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 3);

            if (originalMethod != null && replacementMethod != null)
            {
                callStates.Add(RedirectionHelper.RedirectCalls(originalMethod, replacementMethod));
            }
        }
    }

    public class CustomCargoTruckAI : CarAI
    {
        public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (!LoadingExtension.Instance.despawnEnabled) {
                data.m_flags &= ~Vehicle.Flags.Congestion;
            }

            if ((data.m_flags & Vehicle.Flags.Congestion) != Vehicle.Flags.None)
            {
                Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            }
            else
            {
                if ((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None && (data.m_waitCounter += 1) > 20)
                {
                    this.RemoveOffers(vehicleID, ref data);
                    data.m_flags &= ~Vehicle.Flags.WaitingTarget;
                    data.m_flags |= Vehicle.Flags.GoingBack;
                    data.m_waitCounter = 0;
                    if (!this.StartPathFind(vehicleID, ref data))
                    {
                        data.Unspawn(vehicleID);
                    }
                }
                base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
            }
        }

        private void RemoveOffers(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
            {
                TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                offer.Vehicle = vehicleID;
                if ((data.m_flags & Vehicle.Flags.TransferToSource) != Vehicle.Flags.None)
                {
                    Singleton<TransferManager>.instance.RemoveIncomingOffer((TransferManager.TransferReason)data.m_transferType, offer);
                }
                else if ((data.m_flags & Vehicle.Flags.TransferToTarget) != Vehicle.Flags.None)
                {
                    Singleton<TransferManager>.instance.RemoveOutgoingOffer((TransferManager.TransferReason)data.m_transferType, offer);
                }
            }
        }

        public static void RedirectCalls(List<RedirectCallsState> callStates)
        {
            MethodInfo originalMethod = typeof(CargoTruckAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 3);
            MethodInfo replacementMethod = typeof(CustomCargoTruckAI).GetMethods().FirstOrDefault(m => m.Name == "SimulationStep" && m.GetParameters().Length == 3);

            if (originalMethod != null && replacementMethod != null)
            {
                callStates.Add(RedirectionHelper.RedirectCalls(originalMethod, replacementMethod));
            }
        }
    }
}
