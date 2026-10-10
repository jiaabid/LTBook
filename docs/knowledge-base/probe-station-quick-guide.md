<!-- tool: Probe Station -->
# Probe Station: Quick Operating Guide

> **Fictional sample content.** This guide was written for the ToolBook demo. It is not a real instrument manual. Do not use it to operate real equipment.

## Safety

The parameter analyzer can source up to 100 V. Never touch the probe tips, chuck or sample while an output is enabled. Always switch outputs off before moving probes. Use the grounded wrist strap whenever you handle devices, because electrostatic discharge can destroy gate oxides.

## Mounting the sample

1. Raise all four probe arms using the Z micrometers.
2. Place the sample on the chuck and switch on the chuck vacuum.
3. Close the dark box lid if you are measuring photosensitive devices or very low currents (below 1 nA).

## Landing the probes

1. Focus the microscope on the contact pads at low magnification.
2. Move each probe over its pad with the X/Y micrometers while the tip is still raised.
3. Lower the tip slowly with the Z micrometer until it just touches the pad (the tip shadow meets the tip), then lower by one more small division to make contact. Overdriving the probe scratches pads and wears tips.

## Running an I–V sweep

1. In the analyzer software, open the **I–V Sweep** template.
2. Assign each probe to a channel (SMU1–SMU4) and set its mode (voltage or current source).
3. Set the compliance current. Start with 1 mA for unknown devices to protect them.
4. Set the sweep range and step, then click **Run**. Save the data as CSV to your folder on the network drive.

## Running a C–V measurement

Use the **C–V** template with the capacitance meter connected to probes 1 and 2. Perform an open and a short correction with the probes raised and with the probes on a shorted pad, respectively, before measuring. Typical settings are 100 kHz and a 30 mV AC level.

## Shutting down

Switch off all outputs, raise all probes, switch off the chuck vacuum and remove your sample. Close the software and check out on ToolBook.

## Troubleshooting

**Very noisy low-current measurements.** Close the dark box, make sure the triaxial cables are connected and not moving, and increase the integration time in the analyzer settings.

**A probe tip is bent.** Do not continue with a damaged tip. Report it to lab staff, who keep spare tips.
