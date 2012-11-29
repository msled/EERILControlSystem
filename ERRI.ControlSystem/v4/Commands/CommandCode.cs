namespace EERIL.ControlSystem.v4.Commands {
    public enum CommandCode : byte {
        HorizontalFin = 0x68,
        VerticalFin = 0x76,
        FocusPosition = 0x66,
        BuoyancyPosition = 0x62,
        FinOffset = 0x61,
        Thrust = 0x74,
        Illumination = 0x69,
        PowerConfiguration = 0x70,
        CTD = 0x63
    }
}
