using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nulah.Roomba.Models {

    public class MqttMessage {
        public string Topic { get; set; }
        public Type Type { get; set; }
        public object Payload { get; set; }
        public string Raw { get; set; }
    }

    public class State {
        public Reported reported { get; set; }
    }

    public class Reported {
        public Wifistat wifistat { get; set; }
        public Netinfo netinfo { get; set; }
        public Wlcfg wlcfg { get; set; }
        public Mac mac { get; set; }
        public Svcendpoints svcEndpoints { get; set; }
        public MapUploadAllowed mapUploadAllowed { get; set; }
        public Country country { get; set; }
        public LocalTimeOffset localtimeoffset { get; set; }
        public UtcTime utctime { get; set; }
        public Pose pose { get; set; }
        public CloudEnv cloudEnv { get; set; }
        public BatteryPercent batPct { get; set; }
        public Dock dock { get; set; }
        public Bin bin { get; set; }
        public Audio audio { get; set; }
        public Cleanmissionstatus cleanMissionStatus { get; set; }
        public Language language { get; set; }
        public NoAutoPasses noAutoPasses { get; set; }
        public NoPP noPP { get; set; }
        public EcoCharge ecoCharge { get; set; }
        public VacHigh vacHigh { get; set; }
        public BinPause binPause { get; set; }
        public CarpetBoost carpetBoost { get; set; }
        public OpenOnly openOnly { get; set; }
        public TwoPass twoPass { get; set; }
        public SchedHold schedHold { get; set; }
        public Lastcommand lastCommand { get; set; }
        public Lang[] langs { get; set; }
        public Bbnav bbnav { get; set; }
        public Bbpanic bbpanic { get; set; }
        public Bbpause bbpause { get; set; }
        public Bbmssn bbmssn { get; set; }
        public Bbrstinfo bbrstinfo { get; set; }
        public Cap cap { get; set; }
        public HardwareRevision hardwareRev { get; set; }
        public SKU sku { get; set; }
        public Cleanschedule cleanSchedule { get; set; }
        public Bbchg3 bbchg3 { get; set; }
        public Bbchg bbchg { get; set; }
        public Bbswitch bbswitch { get; set; }
        public Tz tz { get; set; }
        public TimeZone timezone { get; set; }
        public Name name { get; set; }
        public Bbrun bbrun { get; set; }
        public Bbsys bbsys { get; set; }
        public Signal signal { get; set; }
        public BatteryType batteryType { get; set; }
        public SoundVersion soundVer { get; set; }
        public UISoftwareVersion uiSwVer { get; set; }
        public NavSoftwareVersion navSwVer { get; set; }
        public WifiSoftwareVersion wifiSwVer { get; set; }
        public MobilityVersion mobilityVer { get; set; }
        public BootloaderVersion bootloaderVer { get; set; }
        public UmiVersion umiVer { get; set; }
        public SoftwareVersion softwareVer { get; set; }
    }

    public class SKU {
        public string sku { get; set; }
    }

    // The json returned has these all listed as strings, even when they appear to be an int.
    // For future sake, I'm leaving their types as strings until proved otherwise
    public class BatteryType {
        public string batteryType { get; set; }
        public string soundVer { get; set; }
        public string uiSwVer { get; set; }
        public string navSwVer { get; set; }
        public string wifiSwVer { get; set; }
        public string mobilityVer { get; set; }
        public string bootloaderVer { get; set; }
        public string umiVer { get; set; }
        public string softwareVer { get; set; }
    }

    public class SoundVersion {
        public string soundVer { get; set; }
    }
    public class UISoftwareVersion {
        public string uiSwVer { get; set; }
    }
    public class NavSoftwareVersion {
        public string navSwVer { get; set; }
    }
    public class WifiSoftwareVersion {
        public string wifiSwVer { get; set; }
    }
    public class MobilityVersion {
        public string mobilityVer { get; set; }
    }
    public class BootloaderVersion {
        public string bootloaderVer { get; set; }
    }
    public class UmiVersion {
        public string umiVer { get; set; }
    }
    public class SoftwareVersion {
        public string softwareVer { get; set; }
    }

    public class Name {
        public string name { get; set; }
    }

    public class TimeZone {
        public string timezone { get; set; }
    }

    public class HardwareRevision {
        public int hardwareRev { get; set; }
    }

    public class EcoCharge {
        public bool ecoCharge { get; set; }
    }

    public class NoPP {
        public bool noPP { get; set; }
    }
    public class VacHigh {
        public bool vacHigh { get; set; }
        public bool binPause { get; set; }
        public bool carpetBoost { get; set; }
        public bool openOnly { get; set; }
        public bool twoPass { get; set; }
        public bool schedHold { get; set; } // ScheduleHold
        public Lastcommand lastCommand { get; set; }
        [JsonIgnore]
        public Dictionary<string, int> langs { get; set; }
    }

    public class VacHighConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(VacHigh).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            JObject obj = JObject.Load(reader);
            string discriminator = (string)obj["ObjectType"];
            VacHigh ret = obj.ToObject<VacHigh>();

            // Custom conversion on the langs property.
            // The JSON returned is [{"lang1":0},{"lang2",1}, ... ] which the default converter can't deserialise
            // in the end it's just a dictionary 
            var langs = obj["langs"].Children()
                .Select(x => new {
                    key = ( (JProperty)x.First ).Name,
                    value = ( (JProperty)x.First ).Value.Value<int>()
                })
                .ToDictionary(x => x.key, x => x.value);
            ret.langs = langs;

            return ret;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        }
    }

    public class BinPause {
        public bool binPause { get; set; }
    }
    public class CarpetBoost {
        public bool carpetBoost { get; set; }
    }
    public class OpenOnly {
        public bool openOnly { get; set; }
    }
    public class TwoPass {
        public bool twoPass { get; set; }
    }
    public class SchedHold {
        public bool schedHold { get; set; }
    }

    public class NoAutoPasses {
        public bool noAutoPasses { get; set; }

    }

    public class Language {
        public int language { get; set; }
    }
    /*{"batPct":100,"dock":{"known":false},"bin":{"present":true,"full":false},"audio":{"active":false}}*/
    public class BatteryPercent {
        public int batPct { get; set; }
        public Dock dock { get; set; }
        public Bin bin { get; set; }
        public Audio audio { get; set; }
    }

    public class UtcTime {
        public int utctime { get; set; }

    }

    public class LocalTimeOffset {
        public int localtimeoffset { get; set; }
        public int utctime { get; set; }
        public Pose pose { get; set; }
    }

    public class MapUploadAllowed {
        public bool mapUploadAllowed { get; set; }
    }

    public class Mac {
        public string mac { get; set; }
    }

    public class CloudEnv {
        public string cloudEnv { get; set; }
    }

    public class Wifistat {
        public int wifi { get; set; }
        public bool uap { get; set; }
        public int cloud { get; set; }
    }

    public class Netinfo {
        public bool dhcp { get; set; }
        public long addr { get; set; }
        public long mask { get; set; }
        public long gw { get; set; }
        public long dns1 { get; set; }
        public int dns2 { get; set; }
        public string bssid { get; set; }
        public int sec { get; set; }
    }

    public class Wlcfg {
        public int sec { get; set; }
        public string ssid { get; set; }
    }

    public class Svcendpoints {
        public string svcDeplId { get; set; }
    }

    public class Pose {
        public int theta { get; set; }
        public Point point { get; set; }
    }

    public class Point {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class Dock {
        public bool known { get; set; }
    }

    public class Bin {
        public bool present { get; set; }
        public bool full { get; set; }
    }

    public class Audio {
        public bool active { get; set; }
    }

    public class CleanMissionStatusSuper {
        public Cleanmissionstatus cleanmissionstatus { get; set; }
        public int language { get; set; } // I believe this int is the index within langs in VacHigh
        public bool noAutoPasses { get; set; }
        public bool noPP { get; set; }
        public bool ecoCharge { get; set; }
    }

    public class Cleanmissionstatus {
        public string cycle { get; set; }
        public string phase { get; set; }
        public int expireM { get; set; }
        public int rechrgM { get; set; }
        public int error { get; set; }
        public int notReady { get; set; }
        public int mssnM { get; set; }
        public int sqft { get; set; }
        public string initiator { get; set; }
        public int nMssn { get; set; }
    }

    public class Lastcommand {
        public string command { get; set; }
        public int time { get; set; }
        public string initiator { get; set; }
    }

    public class BbNavSuper {
        public Bbnav bbnav { get; set; }
        public Bbpanic bbpanic { get; set; }
        public Bbpause bbpause { get; set; }
        public Bbmssn bbmssn { get; set; }
    }

    public class Bbnav {
        public int aMtrack { get; set; }
        public int nGoodLmrks { get; set; }
        public int aGain { get; set; }
        public int aExpo { get; set; }
    }

    public class Bbpanic {
        public int[] panics { get; set; }
    }

    public class Bbpause {
        public int[] pauses { get; set; }
    }

    public class Bbmssn {
        public int nMssn { get; set; }
        public int nMssnOk { get; set; }
        public int nMssnC { get; set; }
        public int nMssnF { get; set; }
        public int aMssnM { get; set; }
        public int aCycleM { get; set; }
    }

    public class BbrstinfoSuper {
        public Bbrstinfo bbrstinfo { get; set; }
        public Cap cap { get; set; }
        public int hardwareRev { get; set; }
        public string sku { get; set; }
    }

    /*{"bbrstinfo":{"nNavRst":7,"nMobRst":1,"causes":"0000"},"cap":{"pose":1,"ota":2,"multiPass":2,"carpetBoost":1,"pp":1,"binFullDetect":1,"langOta":1,"maps":1,"edge":1,"eco":1,"svcConf":1},"hardwareRev":2,"sku":"R980000"}*/
    public class Bbrstinfo {
        public int nNavRst { get; set; }
        public int nMobRst { get; set; }
        public string causes { get; set; }
    }

    public class Cap {
        public int pose { get; set; }
        public int ota { get; set; }
        public int multiPass { get; set; }
        public int carpetBoost { get; set; }
        public int pp { get; set; }
        public int binFullDetect { get; set; }
        public int langOta { get; set; }
        public int maps { get; set; }
        public int edge { get; set; }
        public int eco { get; set; }
        public int svcConf { get; set; }
    }

    public class CleanScheduleSuper {
        public Cleanschedule cleanSchedule { get; set; }
        public Bbchg3 bbchg3 { get; set; }
    }

    /*{"bbchg3":{"avgMin":440,"hOnDock":2605,"nAvail":152,"estCap":12311,"nLithChrg":52,"nNimhChrg":0,"nDocks":72}}*/
    public class Cleanschedule {
        public string[] cycle { get; set; }
        public int[] h { get; set; }
        public int[] m { get; set; }
    }

    public class Bbchg3 {
        public int avgMin { get; set; }
        public int hOnDock { get; set; }
        public int nAvail { get; set; }
        public int estCap { get; set; }
        public int nLithChrg { get; set; }
        public int nNimhChrg { get; set; }
        public int nDocks { get; set; }
    }

    //{"bbchg":{"nChgOk":51,"nLithF":0,"aborts":[0,0,0]},"bbswitch":{"nBumper":49823,"nClean":85,"nSpot":14,"nDock":72,"nDrops":121}}
    public class BbchgSuper {
        public Bbchg bbchg { get; set; }
        public Bbswitch bbswitch { get; set; }
    }

    public class Bbchg {
        public int nChgOk { get; set; }
        public int nLithF { get; set; }
        public int[] aborts { get; set; }
    }

    public class Bbswitch {
        public int nBumper { get; set; }
        public int nClean { get; set; }
        public int nSpot { get; set; }
        public int nDock { get; set; }
        public int nDrops { get; set; }
    }

    public class TzSuper {
        public Tz tz { get; set; }
        public string timezone { get; set; }
        public string name { get; set; }
    }

    public class Tz {
        public Event[] events { get; set; }
        public int ver { get; set; }
    }

    public class Event {
        public int dt { get; set; }
        public int off { get; set; }
    }

    public class Country {
        public string country { get; set; }
    }

    //{"bbsys":{"hr":2822,"min":4}}
    public class BbrunSuper {
        public Bbrun bbrun { get; set; }
        public Bbsys bbsys { get; set; }
    }

    public class Bbrun {
        public int hr { get; set; }
        public int min { get; set; }
        public int sqft { get; set; }
        public int nStuck { get; set; }
        public int nScrubs { get; set; }
        public int nPicks { get; set; }
        public int nPanics { get; set; }
        public int nCliffsF { get; set; }
        public int nCliffsR { get; set; }
        public int nMBStll { get; set; }
        public int nWStll { get; set; }
        public int nCBump { get; set; }
    }

    public class Bbsys {
        public int hr { get; set; }
        public int min { get; set; }
    }

    public class Signal {
        public int rssi { get; set; }
        public int snr { get; set; }
    }

    public class Lang {
        public int enUK { get; set; }
        public int zhTW { get; set; }
        public int zhHK { get; set; }
    }

}
