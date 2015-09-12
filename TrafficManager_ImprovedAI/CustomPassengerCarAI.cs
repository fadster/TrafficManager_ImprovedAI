using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
    public class CustomPassengerCarAI : CarAI
    {
        public override Color GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
        {
            if (infoMode == InfoManager.InfoMode.Connections) {
                InfoManager.SubInfoMode currentSubMode = Singleton<InfoManager>.instance.CurrentSubMode;
                if (currentSubMode == InfoManager.SubInfoMode.WindPower) {
                    CitizenManager instance = Singleton<CitizenManager>.instance;
                    uint num = data.m_citizenUnits;
                    int num2 = 0;
                    while (num != 0u) {
                        uint nextUnit = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                        for (int i = 0; i < 5; i++) {
                            uint citizen = instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizen(i);
                            if (citizen != 0u && (instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_flags & Citizen.Flags.Tourist) != Citizen.Flags.None) {
                                return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_targetColor;
                            }
                        }
                        num = nextUnit;
                        if (++num2 > 524288) {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
                return base.GetColor(vehicleID, ref data, infoMode);
            }
            if (infoMode != InfoManager.InfoMode.None) {
                return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
            }
            if (!this.m_info.m_useColorVariations) {
                return this.m_info.m_color0;
            }
            Randomizer randomizer = new Randomizer((uint)data.m_transferSize);
            switch (randomizer.Int32(4u)) {
                case 0:
                    return this.m_info.m_color0;
                case 1:
                    return this.m_info.m_color1;
                case 2:
                    return this.m_info.m_color2;
                case 3:
                    return this.m_info.m_color3;
                default:
                    return this.m_info.m_color0;
            }
        }

        public override Color GetColor(ushort parkedVehicleID, ref VehicleParked data, InfoManager.InfoMode infoMode)
        {
            if (infoMode != InfoManager.InfoMode.None) {
                return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
            }
            if (!this.m_info.m_useColorVariations) {
                return this.m_info.m_color0;
            }
            Randomizer randomizer = new Randomizer(data.m_ownerCitizen & 65535u);
            switch (randomizer.Int32(4u)) {
                case 0:
                    return this.m_info.m_color0;
                case 1:
                    return this.m_info.m_color1;
                case 2:
                    return this.m_info.m_color2;
                case 3:
                    return this.m_info.m_color3;
                default:
                    return this.m_info.m_color0;
            }
        }

        public override string GetLocalizedStatus(ushort vehicleID, ref Vehicle data, out InstanceID target)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref data);
            ushort num = 0;
            if (driverInstance != 0) {
                if ((data.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None) {
                    uint citizen = instance.m_instances.m_buffer[(int)driverInstance].m_citizen;
                    if (citizen != 0u && instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_parkedVehicle != 0) {
                        target = InstanceID.Empty;
                        return Locale.Get("VEHICLE_STATUS_PARKING");
                    }
                }
                num = instance.m_instances.m_buffer[(int)driverInstance].m_targetBuilding;
            }
            if (num == 0) {
                target = InstanceID.Empty;
                return Locale.Get("VEHICLE_STATUS_CONFUSED");
            }
            bool flag = (Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)num].m_flags & Building.Flags.IncomingOutgoing) != Building.Flags.None;
            if (flag) {
                target = InstanceID.Empty;
                return Locale.Get("VEHICLE_STATUS_LEAVING");
            }
            target = InstanceID.Empty;
            target.Building = num;
            return Locale.Get("VEHICLE_STATUS_GOINGTO");
        }

        public override string GetLocalizedStatus(ushort parkedVehicleID, ref VehicleParked data, out InstanceID target)
        {
            target = InstanceID.Empty;
            return Locale.Get("VEHICLE_STATUS_PARKED");
        }

        public override void CreateVehicle(ushort vehicleID, ref Vehicle data)
        {
            base.CreateVehicle(vehicleID, ref data);
            Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, 0, vehicleID, 0, 0, 0, 5, 0);
        }

        public override void ReleaseVehicle(ushort vehicleID, ref Vehicle data)
        {
            this.RemoveTarget(vehicleID, ref data);
            base.ReleaseVehicle(vehicleID, ref data);
        }

        public override void LoadVehicle(ushort vehicleID, ref Vehicle data)
        {
            base.LoadVehicle(vehicleID, ref data);
            if (data.m_targetBuilding != 0) {
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].AddGuestVehicle(vehicleID, ref data);
            }
        }

        public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (!LoadingExtension.Instance.despawnEnabled) {
                data.m_flags &= ~Vehicle.Flags.Congestion;
            }

            if ((data.m_flags & Vehicle.Flags.Congestion) != Vehicle.Flags.None) {
                Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            } else {
                base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
            }
        }

        public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None) {
                vehicleData.m_waitCounter += 1;
                if (this.CanLeave(vehicleID, ref vehicleData)) {
                    vehicleData.m_flags &= ~Vehicle.Flags.Stopped;
                    vehicleData.m_waitCounter = 0;
                }
            }
            base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
        }

        public override void SetSource(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            if (sourceBuilding != 0) {
                BuildingManager instance = Singleton<BuildingManager>.instance;
                BuildingInfo info = instance.m_buildings.m_buffer[(int)sourceBuilding].Info;
                data.Unspawn(vehicleID);
                Randomizer randomizer = new Randomizer((int)vehicleID);
                Vector3 vector;
                Vector3 vector2;
                info.m_buildingAI.CalculateSpawnPosition(sourceBuilding, ref instance.m_buildings.m_buffer[(int)sourceBuilding], ref randomizer, this.m_info, out vector, out vector2);
                Quaternion rotation = Quaternion.identity;
                Vector3 forward = vector2 - vector;
                if (forward.sqrMagnitude > 0.01f) {
                    rotation = Quaternion.LookRotation(forward);
                }
                data.m_frame0 = new Vehicle.Frame(vector, rotation);
                data.m_frame1 = data.m_frame0;
                data.m_frame2 = data.m_frame0;
                data.m_frame3 = data.m_frame0;
                data.m_targetPos0 = vector;
                data.m_targetPos0.w = 2f;
                data.m_targetPos1 = vector2;
                data.m_targetPos1.w = 2f;
                data.m_targetPos2 = data.m_targetPos1;
                data.m_targetPos3 = data.m_targetPos1;
                this.FrameDataUpdated(vehicleID, ref data, ref data.m_frame0);
            }
        }

        public override void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            this.RemoveTarget(vehicleID, ref data);
            data.m_targetBuilding = targetBuilding;
            if (targetBuilding != 0) {
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)targetBuilding].AddGuestVehicle(vehicleID, ref data);
            }
            if (!this.StartPathFind(vehicleID, ref data)) {
                data.Unspawn(vehicleID);
            }
        }

        public override void BuildingRelocated(ushort vehicleID, ref Vehicle data, ushort building)
        {
            base.BuildingRelocated(vehicleID, ref data, building);
            if (building == data.m_targetBuilding) {
                this.InvalidPath(vehicleID, ref data, vehicleID, ref data);
            }
        }

        private void RemoveTarget(ushort vehicleID, ref Vehicle data)
        {
            if (data.m_targetBuilding != 0) {
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].RemoveGuestVehicle(vehicleID, ref data);
                data.m_targetBuilding = 0;
            }
        }

        private bool ArriveAtTarget(ushort vehicleID, ref Vehicle data)
        {
            if ((data.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None) {
                VehicleManager instance = Singleton<VehicleManager>.instance;
                CitizenManager instance2 = Singleton<CitizenManager>.instance;
                ushort driverInstance = this.GetDriverInstance(vehicleID, ref data);
                if (driverInstance != 0) {
                    uint citizen = instance2.m_instances.m_buffer[(int)driverInstance].m_citizen;
                    if (citizen != 0u) {
                        ushort parkedVehicle = instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_parkedVehicle;
                        if (parkedVehicle != 0) {
                            Vehicle.Frame lastFrameData = data.GetLastFrameData();
                            instance.m_parkedVehicles.m_buffer[(int)parkedVehicle].m_travelDistance = lastFrameData.m_travelDistance;
                            VehicleParked[] expr_A1_cp_0 = instance.m_parkedVehicles.m_buffer;
                            ushort expr_A1_cp_1 = parkedVehicle;
                            // expr_A1_cp_0[(int)expr_A1_cp_1].m_flags = (expr_A1_cp_0[(int)expr_A1_cp_1].m_flags & 65527);
                            expr_A1_cp_0[(int)expr_A1_cp_1].m_flags = (ushort)(expr_A1_cp_0[(int)expr_A1_cp_1].m_flags & 65527);
                            InstanceID empty = InstanceID.Empty;
                            empty.Vehicle = vehicleID;
                            InstanceID empty2 = InstanceID.Empty;
                            empty2.ParkedVehicle = parkedVehicle;
                            Singleton<InstanceManager>.instance.ChangeInstance(empty, empty2);
                        }
                    }
                }
            }
            this.UnloadPassengers(vehicleID, ref data);
            if (data.m_targetBuilding == 0) {
                return true;
            }
            data.m_targetPos0 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].CalculateSidewalkPosition();
            data.m_targetPos0.w = 2f;
            data.m_targetPos1 = data.m_targetPos0;
            data.m_targetPos2 = data.m_targetPos0;
            data.m_targetPos3 = data.m_targetPos0;
            this.RemoveTarget(vehicleID, ref data);
            return true;
        }

        private void UnloadPassengers(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = data.m_citizenUnits;
            int num2 = 0;
            while (num != 0u) {
                uint nextUnit = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                for (int i = 0; i < 5; i++) {
                    uint citizen = instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizen(i);
                    if (citizen != 0u) {
                        ushort instance2 = instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_instance;
                        if (instance2 != 0) {
                            CitizenInfo info = instance.m_instances.m_buffer[(int)instance2].Info;
                            info.m_citizenAI.SetCurrentVehicle(instance2, ref instance.m_instances.m_buffer[(int)instance2], 0, 0u, data.m_targetPos0);
                        }
                    }
                }
                num = nextUnit;
                if (++num2 > 524288) {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        private ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = data.m_citizenUnits;
            int num2 = 0;
            while (num != 0u) {
                uint nextUnit = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                for (int i = 0; i < 5; i++) {
                    uint citizen = instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizen(i);
                    if (citizen != 0u) {
                        ushort instance2 = instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_instance;
                        if (instance2 != 0) {
                            return instance2;
                        }
                    }
                }
                num = nextUnit;
                if (++num2 > 524288) {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return 0;
        }

        protected override float CalculateTargetSpeed(ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
        {
            return base.CalculateTargetSpeed(vehicleID, ref data, speedLimit, curve);
        }

        public override bool ArriveAtDestination(ushort vehicleID, ref Vehicle vehicleData)
        {
            return this.ArriveAtTarget(vehicleID, ref vehicleData);
        }

        public override void UpdateBuildingTargetPositions(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, ushort leaderID, ref Vehicle leaderData, ref int index, float minSqrDistance)
        {
            if ((leaderData.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None) {
                ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
                if (driverInstance != 0) {
                    CitizenManager instance = Singleton<CitizenManager>.instance;
                    VehicleManager instance2 = Singleton<VehicleManager>.instance;
                    uint citizen = instance.m_instances.m_buffer[(int)driverInstance].m_citizen;
                    if (citizen != 0u) {
                        ushort parkedVehicle = instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_parkedVehicle;
                        if (parkedVehicle != 0) {
                            Vector3 position = instance2.m_parkedVehicles.m_buffer[(int)parkedVehicle].m_position;
                            Quaternion rotation = instance2.m_parkedVehicles.m_buffer[(int)parkedVehicle].m_rotation;
                            vehicleData.SetTargetPos(index++, base.CalculateTargetPoint(refPos, position, minSqrDistance, 2f));
                            if (index < 4) {
                                Vector4 pos = position + rotation * new Vector3(0f, 0f, 0.2f);
                                pos.w = 2f;
                                vehicleData.SetTargetPos(index++, pos);
                            }
                        }
                    }
                }
            }
        }

        protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData)
        {
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0) {
                ushort targetBuilding = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)driverInstance].m_targetBuilding;
                if (targetBuilding != 0) {
                    BuildingManager instance = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance.m_buildings.m_buffer[(int)targetBuilding].Info;
                    Randomizer randomizer = new Randomizer((int)vehicleID);
                    Vector3 vector;
                    Vector3 endPos;
                    info.m_buildingAI.CalculateUnspawnPosition(targetBuilding, ref instance.m_buildings.m_buffer[(int)targetBuilding], ref randomizer, this.m_info, out vector, out endPos);
                    return this.StartPathFind(vehicleID, ref vehicleData, vehicleData.m_targetPos3, endPos);
                }
            }
            return false;
        }

        protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays)
        {
            VehicleInfo info = this.m_info;
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance == 0) {
                return false;
            }
            CitizenManager instance = Singleton<CitizenManager>.instance;
            CitizenInfo info2 = instance.m_instances.m_buffer[(int)driverInstance].Info;
            NetInfo.LaneType laneTypes = NetInfo.LaneType.Vehicle | NetInfo.LaneType.Pedestrian;
            VehicleInfo.VehicleType vehicleType = this.m_info.m_vehicleType;
            bool allowUnderground = (vehicleData.m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None;
            PathUnit.Position startPosA;
            PathUnit.Position startPosB;
            float num;
            float num2;
            PathUnit.Position endPosA;
            if (PathManager.FindPathPosition(startPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, allowUnderground, false, 32f, out startPosA, out startPosB, out num, out num2) && info2.m_citizenAI.FindPathPosition(driverInstance, ref instance.m_instances.m_buffer[(int)driverInstance], endPos, laneTypes, vehicleType, false, out endPosA)) {
                if (!startBothWays || num < 10f) {
                    startPosB = default(PathUnit.Position);
                }
                PathUnit.Position endPosB = default(PathUnit.Position);
                SimulationManager instance2 = Singleton<SimulationManager>.instance;
                uint path;
                if (Singleton<PathManager>.instance.CreatePath(out path, ref instance2.m_randomizer, instance2.m_currentBuildIndex, startPosA, startPosB, endPosA, endPosB, laneTypes, vehicleType, 20000f)) {
                    if (vehicleData.m_path != 0u) {
                        Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
                    }
                    vehicleData.m_path = path;
                    vehicleData.m_flags |= Vehicle.Flags.WaitingPath;
                    return true;
                }
            }
            return false;
        }

        public override bool CanLeave(ushort vehicleID, ref Vehicle vehicleData)
        {
            return vehicleData.m_waitCounter >= 2 && base.CanLeave(vehicleID, ref vehicleData);
        }

        public override InstanceID GetOwnerID(ushort vehicleID, ref Vehicle vehicleData)
        {
            InstanceID result = default(InstanceID);
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0) {
                result.Citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)driverInstance].m_citizen;
            }
            return result;
        }

        public override InstanceID GetTargetID(ushort vehicleID, ref Vehicle vehicleData)
        {
            InstanceID result = default(InstanceID);
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0) {
                result.Building = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)driverInstance].m_targetBuilding;
            }
            return result;
        }

        protected override bool ParkVehicle(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, out byte segmentOffset)
        {
            PathManager instance = Singleton<PathManager>.instance;
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            NetManager instance3 = Singleton<NetManager>.instance;
            VehicleManager instance4 = Singleton<VehicleManager>.instance;
            uint num = 0u;
            uint num2 = vehicleData.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0u && num == 0u) {
                uint nextUnit = instance2.m_units.m_buffer[(int)((UIntPtr)num2)].m_nextUnit;
                for (int i = 0; i < 5; i++) {
                    uint citizen = instance2.m_units.m_buffer[(int)((UIntPtr)num2)].GetCitizen(i);
                    if (citizen != 0u) {
                        ushort instance5 = instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_instance;
                        if (instance5 != 0) {
                            num = instance2.m_instances.m_buffer[(int)instance5].m_citizen;
                            break;
                        }
                    }
                }
                num2 = nextUnit;
                if (++num3 > 524288) {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            if (num != 0u) {
                uint laneID = PathManager.GetLaneID(pathPos);
                segmentOffset = (byte)Singleton<SimulationManager>.instance.m_randomizer.Int32(1, 254);
                Vector3 refPos;
                Vector3 vector;
                instance3.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CalculatePositionAndDirection((float)segmentOffset * 0.003921569f, out refPos, out vector);
                NetInfo info = instance3.m_segments.m_buffer[(int)pathPos.m_segment].Info;
                bool flag = (instance3.m_segments.m_buffer[(int)pathPos.m_segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
                bool flag2 = info.m_lanes[(int)pathPos.m_lane].m_position < 0f;
                vector.Normalize();
                Vector3 searchDir;
                if (flag != flag2) {
                    searchDir.x = -vector.z;
                    searchDir.y = 0f;
                    searchDir.z = vector.x;
                } else {
                    searchDir.x = vector.z;
                    searchDir.y = 0f;
                    searchDir.z = -vector.x;
                }
                ushort homeID = 0;
                if (num != 0u) {
                    homeID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[(int)((UIntPtr)num)].m_homeBuilding;
                }
                Vector3 position;
                Quaternion rotation;
                float num4;
                ushort parkedVehicleID;
                if (pathPos.m_segment == TrafficLightTool.parkingSegment) {
                    Debug.Log("finding parking space for " + vehicleID);
                }
                if (CustomPassengerCarAI.FindParkingSpace(homeID, refPos, searchDir, pathPos.m_segment, this.m_info.m_generatedInfo.m_size.x, this.m_info.m_generatedInfo.m_size.z, out position, out rotation, out num4) && instance4.CreateParkedVehicle(out parkedVehicleID, ref Singleton<SimulationManager>.instance.m_randomizer, this.m_info, position, rotation, num)) {
                    instance2.m_citizens.m_buffer[(int)((UIntPtr)num)].SetParkedVehicle(num, parkedVehicleID);
                    if (num4 >= 0f) {
                        segmentOffset = (byte)(num4 * 255f);
                    }
                }
            } else {
                segmentOffset = pathPos.m_offset;
            }
            if (num != 0u) {
                uint num5 = vehicleData.m_citizenUnits;
                int num6 = 0;
                while (num5 != 0u) {
                    uint nextUnit2 = instance2.m_units.m_buffer[(int)((UIntPtr)num5)].m_nextUnit;
                    for (int j = 0; j < 5; j++) {
                        uint citizen2 = instance2.m_units.m_buffer[(int)((UIntPtr)num5)].GetCitizen(j);
                        if (citizen2 != 0u) {
                            ushort instance6 = instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen2)].m_instance;
                            if (instance6 != 0 && instance.AddPathReference(nextPath)) {
                                if (instance2.m_instances.m_buffer[(int)instance6].m_path != 0u) {
                                    instance.ReleasePath(instance2.m_instances.m_buffer[(int)instance6].m_path);
                                }
                                instance2.m_instances.m_buffer[(int)instance6].m_path = nextPath;
                                instance2.m_instances.m_buffer[(int)instance6].m_pathPositionIndex = (byte)nextPositionIndex;
                                instance2.m_instances.m_buffer[(int)instance6].m_lastPathOffset = segmentOffset;
                            }
                        }
                    }
                    num5 = nextUnit2;
                    if (++num6 > 524288) {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
            return true;
        }

        public override void UpdateParkedVehicle(ushort parkedID, ref VehicleParked parkedData)
        {
            float x = this.m_info.m_generatedInfo.m_size.x;
            float z = this.m_info.m_generatedInfo.m_size.z;
            float num = 256f;
            bool flag = false;
            uint ownerCitizen = parkedData.m_ownerCitizen;
            ushort homeID = 0;
            if (ownerCitizen != 0u) {
                homeID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[(int)((UIntPtr)ownerCitizen)].m_homeBuilding;
            }
            Vector3 vector;
            Quaternion rotation;
            float num2;
            if (CustomPassengerCarAI.FindParkingSpaceRoadSide(parkedID, 0, parkedData.m_position, x - 0.2f, z, out vector, out rotation, out num2)) {
                float num3 = Vector3.SqrMagnitude(vector - parkedData.m_position);
                if (num3 < num) {
                    num = num3;
                    flag = true;
                }
            }
            Vector3 vector2;
            Quaternion quaternion;
            if (CustomPassengerCarAI.FindParkingSpaceBuilding(homeID, parkedID, parkedData.m_position, x, z, num, out vector2, out quaternion)) {
                vector = vector2;
                rotation = quaternion;
                flag = true;
            }
            if (flag) {
                Singleton<VehicleManager>.instance.RemoveFromGrid(parkedID, ref parkedData);
                parkedData.m_position = vector;
                parkedData.m_rotation = rotation;
                Singleton<VehicleManager>.instance.AddToGrid(parkedID, ref parkedData);
            } else {
                Singleton<VehicleManager>.instance.ReleaseParkedVehicle(parkedID);
            }
        }

        private static bool FindParkingSpace(ushort homeID, Vector3 refPos, Vector3 searchDir, ushort segment, float width, float length, out Vector3 parkPos, out Quaternion parkRot, out float parkOffset)
        {
            if (segment == TrafficLightTool.parkingSegment) {
                Debug.Log("finding parking space for ");
            }

            Vector3 refPos2 = refPos + searchDir * 16f;
            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(3u) == 0) {
                if (CustomPassengerCarAI.FindParkingSpaceRoadSide(0, segment, refPos, width - 0.2f, length, out parkPos, out parkRot, out parkOffset)) {
                    if (segment == TrafficLightTool.parkingSegment) {
                        Debug.Log("found roadside spot (1)");
                    }
                    return true;
                }
                if (CustomPassengerCarAI.FindParkingSpaceBuilding(homeID, 0, refPos2, width, length, 16f, out parkPos, out parkRot)) {
                    parkOffset = -1f;
                    if (segment == TrafficLightTool.parkingSegment) {
                        Debug.Log("found building spot (1)");
                    }
                    return true;
                }
            } else {
                if (CustomPassengerCarAI.FindParkingSpaceBuilding(homeID, 0, refPos2, width, length, 16f, out parkPos, out parkRot)) {
                    parkOffset = -1f;
                    if (segment == TrafficLightTool.parkingSegment) {
                        Debug.Log("found building spot (2) for ");
                    }
                    return true;
                }
                if (CustomPassengerCarAI.FindParkingSpaceRoadSide(0, segment, refPos, width - 0.2f, length, out parkPos, out parkRot, out parkOffset)) {
                    if (segment == TrafficLightTool.parkingSegment) {
                        Debug.Log("found roadside spot (2) for ");
                    }
                    return true;
                }
            }
            if (segment == TrafficLightTool.parkingSegment) {
                Debug.Log("couldn't find spot for ");
            }
            return false;
        }

        private static bool FindParkingSpaceBuilding(ushort homeID, ushort ignoreParked, Vector3 refPos, float width, float length, float maxDistance, out Vector3 parkPos, out Quaternion parkRot)
        {
            parkPos = Vector3.zero;
            parkRot = Quaternion.identity;
            float num = refPos.x - maxDistance;
            float num2 = refPos.z - maxDistance;
            float num3 = refPos.x + maxDistance;
            float num4 = refPos.z + maxDistance;
            int num5 = Mathf.Max((int)((num - 72f) / 64f + 135f), 0);
            int num6 = Mathf.Max((int)((num2 - 72f) / 64f + 135f), 0);
            int num7 = Mathf.Min((int)((num3 + 72f) / 64f + 135f), 269);
            int num8 = Mathf.Min((int)((num4 + 72f) / 64f + 135f), 269);
            BuildingManager instance = Singleton<BuildingManager>.instance;
            bool result = false;
            for (int i = num6; i <= num8; i++) {
                for (int j = num5; j <= num7; j++) {
                    ushort num9 = instance.m_buildingGrid[i * 270 + j];
                    int num10 = 0;
                    while (num9 != 0) {
                        if (CustomPassengerCarAI.FindParkingSpaceBuilding(homeID, ignoreParked, num9, ref instance.m_buildings.m_buffer[(int)num9], refPos, width, length, ref maxDistance, ref parkPos, ref parkRot)) {
                            result = true;
                        }
                        num9 = instance.m_buildings.m_buffer[(int)num9].m_nextGridBuilding;
                        if (++num10 >= 32768) {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private static bool FindParkingSpaceBuilding(ushort homeID, ushort ignoreParked, ushort buildingID, ref Building building, Vector3 refPos, float width, float length, ref float maxDistance, ref Vector3 parkPos, ref Quaternion parkRot)
        {
            int width2 = building.Width;
            int length2 = building.Length;
            float num = Mathf.Sqrt((float)(width2 * width2 + length2 * length2)) * 8f;
            if (VectorUtils.LengthXZ(building.m_position - refPos) >= maxDistance + num) {
                return false;
            }
            if ((building.m_flags & Building.Flags.BurnedDown) != Building.Flags.None) {
                return false;
            }
            BuildingInfo info = building.Info;
            Matrix4x4 matrix4x = default(Matrix4x4);
            bool flag = false;
            bool result = false;
            if (info.m_class.m_service == ItemClass.Service.Residential && buildingID != homeID) {
                return result;
            }
            if (info.m_props != null) {
                for (int i = 0; i < info.m_props.Length; i++) {
                    BuildingInfo.Prop prop = info.m_props[i];
                    Randomizer randomizer = new Randomizer((int)buildingID << 6 | prop.m_index);
                    if (randomizer.Int32(100u) < prop.m_probability && length2 >= prop.m_requiredLength) {
                        PropInfo propInfo = prop.m_finalProp;
                        if (propInfo != null) {
                            propInfo = propInfo.GetVariation(ref randomizer);
                            if (propInfo.m_parkingSpaces != null && propInfo.m_parkingSpaces.Length != 0) {
                                if (!flag) {
                                    flag = true;
                                    Vector3 pos = Building.CalculateMeshPosition(info, building.m_position, building.m_angle, building.Length);
                                    Quaternion q = Quaternion.AngleAxis(building.m_angle * 57.29578f, Vector3.down);
                                    matrix4x.SetTRS(pos, q, Vector3.one);
                                }
                                Vector3 position = matrix4x.MultiplyPoint(prop.m_position);
                                if (CustomPassengerCarAI.FindParkingSpaceProp(ignoreParked, propInfo, position, building.m_angle + prop.m_radAngle, prop.m_fixedHeight, refPos, width, length, ref maxDistance, ref parkPos, ref parkRot)) {
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static bool FindParkingSpaceProp(ushort ignoreParked, PropInfo info, Vector3 position, float angle, bool fixedHeight, Vector3 refPos, float width, float length, ref float maxDistance, ref Vector3 parkPos, ref Quaternion parkRot)
        {
            bool result = false;
            Matrix4x4 matrix4x = default(Matrix4x4);
            Quaternion q = Quaternion.AngleAxis(angle * 57.29578f, Vector3.down);
            matrix4x.SetTRS(position, q, Vector3.one);
            for (int i = 0; i < info.m_parkingSpaces.Length; i++) {
                Vector3 vector = matrix4x.MultiplyPoint(info.m_parkingSpaces[i].m_position);
                float num = Vector3.Distance(vector, refPos);
                if (num < maxDistance) {
                    float d = (info.m_parkingSpaces[i].m_size.z - length) * 0.5f;
                    Vector3 vector2 = matrix4x.MultiplyVector(info.m_parkingSpaces[i].m_direction);
                    vector += vector2 * d;
                    if (fixedHeight) {
                        Vector3 b = vector2 * (length * 0.5f - 1f);
                        Segment3 segment = new Segment3(vector + b, vector - b);
                        if (!CustomPassengerCarAI.CheckOverlap(ignoreParked, segment)) {
                            parkPos = vector;
                            parkRot = Quaternion.LookRotation(vector2);
                            maxDistance = num;
                            result = true;
                        }
                    } else {
                        Vector3 vector3 = vector + new Vector3(vector2.x * length * 0.25f + vector2.z * width * 0.4f, 0f, vector2.z * length * 0.25f - vector2.x * width * 0.4f);
                        Vector3 vector4 = vector + new Vector3(vector2.x * length * 0.25f - vector2.z * width * 0.4f, 0f, vector2.z * length * 0.25f + vector2.x * width * 0.4f);
                        Vector3 vector5 = vector - new Vector3(vector2.x * length * 0.25f - vector2.z * width * 0.4f, 0f, vector2.z * length * 0.25f + vector2.x * width * 0.4f);
                        Vector3 vector6 = vector - new Vector3(vector2.x * length * 0.25f + vector2.z * width * 0.4f, 0f, vector2.z * length * 0.25f - vector2.x * width * 0.4f);
                        vector3.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector3);
                        vector4.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector4);
                        vector5.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector5);
                        vector6.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector6);
                        vector.y = (vector3.y + vector4.y + vector5.y + vector6.y) * 0.25f;
                        Vector3 normalized = (vector3 + vector4 - vector5 - vector6).normalized;
                        Vector3 b2 = normalized * (length * 0.5f - 1f);
                        Segment3 segment2 = new Segment3(vector + b2, vector - b2);
                        if (!CustomPassengerCarAI.CheckOverlap(ignoreParked, segment2)) {
                            Vector3 rhs = vector3 + vector5 - vector4 - vector6;
                            parkPos = vector;
                            parkRot = Quaternion.LookRotation(normalized, Vector3.Cross(normalized, rhs));
                            maxDistance = num;
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        private static bool CheckOverlap(ushort ignoreParked, Segment3 segment)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            Vector3 vector = segment.Min();
            Vector3 vector2 = segment.Max();
            int num = Mathf.Max((int)((vector.x - 10f) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((vector.z - 10f) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((vector2.x + 10f) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((vector2.z + 10f) / 32f + 270f), 539);
            bool result = false;
            for (int i = num2; i <= num4; i++) {
                for (int j = num; j <= num3; j++) {
                    ushort num5 = instance.m_parkedGrid[i * 540 + j];
                    int num6 = 0;
                    while (num5 != 0) {
                        num5 = CustomPassengerCarAI.CheckOverlap(ignoreParked, segment, num5, ref instance.m_parkedVehicles.m_buffer[(int)num5], ref result);
                        if (++num6 > 32768) {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private static ushort CheckOverlap(ushort ignoreParked, Segment3 segment, ushort otherID, ref VehicleParked otherData, ref bool overlap)
        {
            if (otherID != ignoreParked) {
                VehicleInfo info = otherData.Info;
                Vector3 b = otherData.m_rotation * new Vector3(0f, 0f, info.m_generatedInfo.m_size.z * 0.5f - 1f);
                Segment3 segment2 = new Segment3(otherData.m_position + b, otherData.m_position - b);
                float num;
                float num2;
                if (segment.DistanceSqr(segment2, out num, out num2) < 1f) {
                    overlap = true;
                }
            }
            return otherData.m_nextGridParked;
        }

        private static bool FindParkingSpaceRoadSide(ushort ignoreParked, ushort requireSegment, Vector3 refPos, float width, float length, out Vector3 parkPos, out Quaternion parkRot, out float parkOffset)
        {
            parkPos = Vector3.zero;
            parkRot = Quaternion.identity;
            parkOffset = 0f;
            PathUnit.Position pathPos;
            if (PathManager.FindPathPosition(refPos, ItemClass.Service.Road, NetInfo.LaneType.Parking, VehicleInfo.VehicleType.Car, false, false, 32f, out pathPos)) {
                if (requireSegment != 0 && pathPos.m_segment != requireSegment) {
                    return false;
                }
                NetManager instance = Singleton<NetManager>.instance;
                NetInfo info = instance.m_segments.m_buffer[(int)pathPos.m_segment].Info;
                uint laneID = PathManager.GetLaneID(pathPos);
                uint num = instance.m_segments.m_buffer[(int)pathPos.m_segment].m_lanes;
                int num2 = 0;
                while (num2 < info.m_lanes.Length && num != 0u) {
                    if ((instance.m_lanes.m_buffer[(int)((UIntPtr)num)].m_flags & 256) != 0 && info.m_lanes[(int)pathPos.m_lane].m_position >= 0f == info.m_lanes[num2].m_position >= 0f) {
                        return false;
                    }
                    num = instance.m_lanes.m_buffer[(int)((UIntPtr)num)].m_nextLane;
                    num2++;
                }
                bool flag = (instance.m_segments.m_buffer[(int)pathPos.m_segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
                bool flag2 = (byte)(info.m_lanes[(int)pathPos.m_lane].m_finalDirection & NetInfo.Direction.Forward) == 0;
                bool flag3 = info.m_lanes[(int)pathPos.m_lane].m_position < 0f;
                float num3 = (float)pathPos.m_offset * 0.003921569f;
                float num4;
                float num5;
                if (CustomPassengerCarAI.CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].m_bezier, num3, length, out num4, out num5)) {
                    num3 = -1f;
                    for (int i = 0; i < 6; i++) {
                        if (num5 <= 1f) {
                            float num6;
                            float num7;
                            if (!CustomPassengerCarAI.CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].m_bezier, num5, length, out num6, out num7)) {
                                num3 = num5;
                                break;
                            }
                            num5 = num7;
                        }
                        if (num4 >= 0f) {
                            float num6;
                            float num7;
                            if (!CustomPassengerCarAI.CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].m_bezier, num4, length, out num6, out num7)) {
                                num3 = num4;
                                break;
                            }
                            num4 = num6;
                        }
                    }
                }
                if (num3 >= 0f) {
                    Vector3 vector;
                    Vector3 vector2;
                    instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CalculatePositionAndDirection(num3, out vector, out vector2);
                    float num8 = (info.m_lanes[(int)pathPos.m_lane].m_width - width) * 0.5f;
                    vector2.Normalize();
                    if (flag != flag3) {
                        parkPos.x = vector.x - vector2.z * num8;
                        parkPos.y = vector.y;
                        parkPos.z = vector.z + vector2.x * num8;
                    } else {
                        parkPos.x = vector.x + vector2.z * num8;
                        parkPos.y = vector.y;
                        parkPos.z = vector.z - vector2.x * num8;
                    }
                    if (flag != flag2) {
                        parkRot = Quaternion.LookRotation(-vector2);
                    } else {
                        parkRot = Quaternion.LookRotation(vector2);
                    }
                    parkOffset = num3;
                    return true;
                }
            }
            return false;
        }

        private static bool CheckOverlap(ushort ignoreParked, ref Bezier3 bezier, float offset, float length, out float minPos, out float maxPos)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            float num = bezier.Travel(offset, length * -0.5f);
            float num2 = bezier.Travel(offset, length * 0.5f);
            bool result = false;
            minPos = offset;
            maxPos = offset;
            if (num < 0.001f) {
                result = true;
                num = 0f;
                minPos = -1f;
                maxPos = Mathf.Max(maxPos, bezier.Travel(0f, length * 0.5f + 0.5f));
            }
            if (num2 > 0.999f) {
                result = true;
                num2 = 1f;
                maxPos = 2f;
                minPos = Mathf.Min(minPos, bezier.Travel(1f, length * -0.5f - 0.5f));
            }
            Vector3 pos = bezier.Position(offset);
            Vector3 dir = bezier.Tangent(offset);
            Vector3 lhs = bezier.Position(num);
            Vector3 rhs = bezier.Position(num2);
            Vector3 vector = Vector3.Min(lhs, rhs);
            Vector3 vector2 = Vector3.Max(lhs, rhs);
            int num3 = Mathf.Max((int)((vector.x - 10f) / 32f + 270f), 0);
            int num4 = Mathf.Max((int)((vector.z - 10f) / 32f + 270f), 0);
            int num5 = Mathf.Min((int)((vector2.x + 10f) / 32f + 270f), 539);
            int num6 = Mathf.Min((int)((vector2.z + 10f) / 32f + 270f), 539);
            for (int i = num4; i <= num6; i++) {
                for (int j = num3; j <= num5; j++) {
                    ushort num7 = instance.m_parkedGrid[i * 540 + j];
                    int num8 = 0;
                    while (num7 != 0) {
                        num7 = CustomPassengerCarAI.CheckOverlap(ignoreParked, ref bezier, pos, dir, offset, length, num7, ref instance.m_parkedVehicles.m_buffer[(int)num7], ref result, ref minPos, ref maxPos);
                        if (++num8 > 32768) {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private static ushort CheckOverlap(ushort ignoreParked, ref Bezier3 bezier, Vector3 pos, Vector3 dir, float offset, float length, ushort otherID, ref VehicleParked otherData, ref bool overlap, ref float minPos, ref float maxPos)
        {
            if (otherID != ignoreParked) {
                VehicleInfo info = otherData.Info;
                Vector3 position = otherData.m_position;
                Vector3 lhs = position - pos;
                float num = (length + info.m_generatedInfo.m_size.z) * 0.5f + 1f;
                float magnitude = lhs.magnitude;
                if (magnitude < num - 0.5f) {
                    overlap = true;
                    float distance;
                    float num2;
                    if (Vector3.Dot(lhs, dir) >= 0f) {
                        distance = num + magnitude;
                        num2 = num - magnitude;
                    } else {
                        distance = num - magnitude;
                        num2 = num + magnitude;
                    }
                    maxPos = Mathf.Max(maxPos, bezier.Travel(offset, distance));
                    minPos = Mathf.Min(minPos, bezier.Travel(offset, -num2));
                }
            }
            return otherData.m_nextGridParked;
        }
    }
}