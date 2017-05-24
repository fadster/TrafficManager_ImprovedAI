using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using System.Collections.Generic;

namespace TimeWarpMod
{

    public class i18n
    {
        private Dictionary<string, string> _ = new Dictionary<string, string>();
        public string lang;
        private static i18n _current;
        private static i18n english = new i18n("en");

        public string this[string key]
        {
            get {
                string value;
                if (!_.TryGetValue(key.ToUpper(), out value))
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, "No translation for string: " + key);
                    return "#" + key;
                }
                return value;    
            }
        }

        public static i18n current
        {
            get
            {
                if (_current == null || !LocaleManager.instance.language.Equals(_current.lang))
                {
                    _current = new i18n(LocaleManager.instance.language);
                }
                return _current;
            }
        }

        private i18n(string selectedLanguage)
        {
            lang = selectedLanguage;
            switch (selectedLanguage)
            {
                
                case "nl":
                    _["MOD_NAME"]               = "Tijdsprong";
                    _["MOD_DESCRIPTION"]        = "Rechtermuisknop op de gebiedenknop om het moment van de dag te veranderen";

                    _["TOGGLE_TOOLTIP"]         = "Dag-/nachtinstellingen";
                    _["ZOOMBUTTON_TOOLTIP"]     = "Gebieden \n Rechtermuisknop om het moment van de dag te veranderen";
                    _["SUNCONTROL"]             = "Zonbediening";
                    _["SUNCONTROL_TITLE"]       = "Dag-/nachtinstellingen";
                    _["SUNCONTROL_SIZE"]        = "Zongrootte";
                    _["SUNCONTROL_INTENSITY"]   = "Zonne-intensiteit";
                    _["LATTITUDE"]              = "Breedtegraad: ";
                    _["LONGITUDE"]              = "Lengtegraad: ";
                    _["SPEED_PAUZED"]           = "Gepauzeerd";
                    _["SPEED_NORMAL"]           = "Normaal";
                    _["SPEED_DISABLED"]         = "Gehandicapte";
                    _["SPEED"]                  = "Snelheid: ";
                    _["NIGHT_DISABLED"]         = "Nachtcyclus uitgeschakeld in instellingen";

                    return;
                case "de":
                    _["MOD_NAME"]               = "Zeitsprung";
                    _["MOD_DESCRIPTION"]        = "Rechtsklick auf dem Gebietenknopf, um die Tageszeit zu ändern";

                    _["TOGGLE_TOOLTIP"]         = "Tag / Nacht-einstellungen";
                    _["ZOOMBUTTON_TOOLTIP"]     = "Gebiete \n Rechtsklick um die Tageszeit zu ändern";
                    _["SUNCONTROL"]             = "Sonnebedienung";
                    _["SUNCONTROL_TITLE"]       = "Tag / Nacht-einstellungen";
                    _["SUNCONTROL_SIZE"]        = "Sonnegröße";
                    _["SUNCONTROL_INTENSITY"]   = "Sonnenintensität";
                    _["LATTITUDE"]              = "Breitengrad: ";
                    _["LONGITUDE"]              = "Längengrad: ";
                    _["SPEED_PAUZED"]           = "Pausiert";
                    _["SPEED_NORMAL"]           = "Normal";
                    _["SPEED_DISABLED"]         = "Behindert";
                    _["SPEED"]                  = "Geschwindigkeit: ";
                    _["NIGHT_DISABLED"]         = "Nacht-Zyklus in den Einstellungen deaktiviert";

                    return;
                case "it":

                    _["MOD_NAME"]               = "Distorsione del tempo";
                    _["MOD_DESCRIPTION"]        = "Tasto destro del mouse sul bottone aree per cambiare l'ora del giorno";

                    _["TOGGLE_TOOLTIP"]         = "Impostazioni giorno / notte";
                    _["ZOOMBUTTON_TOOLTIP"]     = "Aree \n Tasto destro del mouse per cambiare l'ora del giorno";
                    _["SUNCONTROL"]             = "Controllo del sole";
                    _["SUNCONTROL_TITLE"]       = "Impostazioni giorno / notte";
                    _["SUNCONTROL_SIZE"]        = "Formato sole";
                    _["SUNCONTROL_INTENSITY"]   = "Intensità solare";
                    _["LATTITUDE"]              = "Lattitudine: ";
                    _["LONGITUDE"]              = "Longitudine: ";
                    _["SPEED_PAUZED"]           = "In pausa";
                    _["SPEED_NORMAL"]           = "Normale";
                    _["SPEED_DISABLED"]         = "Disabilitato";
                    _["SPEED"]                  = "Velocità: ";
                    _["NIGHT_DISABLED"]         = "Ciclo Notte disattivata nelle impostazioni";

                    return;

                default:
                    _["MOD_NAME"]               = "Time Warp";
                    _["MOD_DESCRIPTION"]        = "Right click on the Area Zoom button to set the time of day";

                    _["TOGGLE_TOOLTIP"]         = "Day/Night Settings";
                    _["ZOOMBUTTON_TOOLTIP"]     = "Areas \n Right Click to set time of day";
                    _["SUNCONTROL"]             = "Sun control";
                    _["SUNCONTROL_TITLE"]       = "Day/Night Settings";
                    _["SUNCONTROL_SIZE"]        = "Sun Size";
                    _["SUNCONTROL_INTENSITY"]   = "Sun Intensity";
                    _["LATTITUDE"]              = "Lattitude: ";
                    _["LONGITUDE"]              = "Longitude: ";
                    _["SPEED_PAUZED"]           = "Paused";
                    _["SPEED_NORMAL"]           = "Normal";
                    _["SPEED_DISABLED"]         = "Disabled";
                    _["SPEED"]                  = "Speed: ";
            
                    _["NIGHT_DISABLED"]         = "Night cycle disabled in settings";

                    return;
            }
            
        }
    }
}
