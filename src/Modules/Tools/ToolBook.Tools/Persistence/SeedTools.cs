using ToolBook.Tools.Domain;

namespace ToolBook.Tools.Persistence;

/// <summary>Demo catalog, applied through the initial migration. Stable ids keep the seed idempotent.</summary>
internal static class SeedTools
{
    public static readonly Tool[] All =
    [
        Tool.Seed(
            Guid.Parse("0199c3a0-0000-7000-8000-000000000001"),
            "Scanning Electron Microscope",
            "Microscopy",
            "Cleanroom B, Room 1.12",
            "Field-emission SEM for high-resolution surface imaging. Secondary and backscattered electron detectors, EDX available.",
            defaultSlotMinutes: 60,
            typicalBookingMinutes: 120),
        Tool.Seed(
            Guid.Parse("0199c3a0-0000-7000-8000-000000000002"),
            "Spectroscopic Ellipsometer",
            "Optical metrology",
            "Lab 2.04",
            "Variable-angle spectroscopic ellipsometer for thin-film thickness and optical constants (190–1700 nm).",
            defaultSlotMinutes: 30,
            typicalBookingMinutes: 45),
        Tool.Seed(
            Guid.Parse("0199c3a0-0000-7000-8000-000000000003"),
            "Probe Station",
            "Electrical characterization",
            "Lab 2.10",
            "Four-probe manual station with a semiconductor parameter analyzer for I–V and C–V measurements.",
            defaultSlotMinutes: 60,
            typicalBookingMinutes: 90),
        Tool.Seed(
            Guid.Parse("0199c3a0-0000-7000-8000-000000000004"),
            "Atomic Force Microscope",
            "Microscopy",
            "Lab 1.07",
            "AFM with tapping and contact modes for nanoscale topography and roughness measurements.",
            defaultSlotMinutes: 60,
            typicalBookingMinutes: 120),
        Tool.Seed(
            Guid.Parse("0199c3a0-0000-7000-8000-000000000005"),
            "Rapid Thermal Annealer",
            "Thermal processing",
            "Cleanroom A, Room 0.03",
            "RTA for wafers up to 150 mm, up to 1200 °C under N2, Ar or forming gas.",
            defaultSlotMinutes: 30,
            typicalBookingMinutes: 40),
    ];
}
