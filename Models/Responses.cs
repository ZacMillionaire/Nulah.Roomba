using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nulah.Roomba.Models.Responses {
    public class CloudEnv {
        public string cloudEnv { get; set; }
    }
    public class WifiStat {
        public int wifi { get; set; }
        public bool uap { get; set; }
        public int cloud { get; set; }
    }
    public class Country {
        public string country { get; set; }
    }
    public class NetInfo {
        public bool dhcp { get; set; }
        public long addr { get; set; }
        public long mask { get; set; }
        public long gw { get; set; }
        public long dns1 { get; set; }
        public int dns2 { get; set; }
        public string bssid { get; set; }
        public int sec { get; set; }
    }
    public class SvcEndPoints {
        public string svcDeplId { get; set; }
    }
    public class Wlcfg {
        public int sec { get; set; }
        public string ssid { get; set; }
    }
    public class Mac {
        public string mac { get; set; }
    }
    public class LocalTimeOffset {
        public int localtimeoffset { get; set; }
        public int utctime { get; set; }
        public Pose pose { get; set; }
    }
    public class MapUploadAllowed {
        public bool mapUploadAllowed { get; set; }
    }
    public class UtcTime {
        public int utctime { get; set; }
    }
    public class Pose {
        public int theta { get; set; }
        public Point point { get; set; }
    }
    public class BatPct {
        public int batPct { get; set; }
        public Dock dock { get; set; }
        public Bin bin { get; set; }
        public Audio audio { get; set; }
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
    public class Language {
        public int language { get; set; }
    }
    public class NoAutoPasses {
        public bool noAutoPasses { get; set; }
    }
    public class NoPP {
        public bool noPP { get; set; }
    }
    public class EcoCharge {
        public bool ecoCharge { get; set; }
    }
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
    public class HardwareRev {
        public int hardwareRev { get; set; }
    }
    public class Sku {
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
    public class SoundVer {
        public string soundVer { get; set; }
    }
    public class UISwVer {
        public string uiSwVer { get; set; }
    }
    public class NavSwVer {
        public string navSwVer { get; set; }
    }
    public class WifiSwVer {
        public string wifiSwVer { get; set; }
    }
    public class MobilityVer {
        public string mobilityVer { get; set; }
    }
    public class BootloaderVer {
        public string bootloaderVer { get; set; }
    }
    public class UmiVer {
        public string umiVer { get; set; }
    }
    public class SoftwareVer {
        public string softwareVer { get; set; }
    }
    public class Tz {
        public Event[] events { get; set; }
        public int ver { get; set; }
    }
    public class Event {
        public int dt { get; set; }
        public int off { get; set; }
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
    public class Langs {
        public int enUK { get; set; }
        public int zhTW { get; set; }
        public int zhHK { get; set; }
    }

    public class LangsConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
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
    public class Lastcommand {
        public string command { get; set; }
        public int time { get; set; }
        public string initiator { get; set; }
    }
    public class VacHigh {
        public bool vacHigh { get; set; }
    }
    public class Name {
        public string name { get; set; }
    }

    public class TimeZone {
        public string timezone { get; set; }
    }
}
