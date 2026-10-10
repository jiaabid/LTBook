<!-- tool: Rapid Thermal Annealer -->
# Rapid Thermal Annealer: Quick Operating Guide

> **Fictional sample content.** This guide was written for the ToolBook demo. It is not a real instrument manual. Do not use it to operate real equipment.

## Safety

The process chamber reaches up to 1200 °C and the quartz tray stays hot for several minutes after a run. Always use the quartz tweezers and wait until the chamber temperature reads below 100 °C before opening the door. Forming gas contains hydrogen. Check that the gas detector on the wall shows a green light before every session, and never run forming gas if the detector shows an alarm.

## Allowed materials

Only silicon, silicon oxide, silicon nitride and the metals listed on the approved-materials sheet next to the tool may be annealed. Gold, copper and organic materials (photoresist, polymers) are **not allowed** because they contaminate the chamber for other users. Ask lab staff before annealing anything not on the list.

## Loading wafers

1. Check that the chamber temperature is below 100 °C.
2. Open the door and pull out the quartz tray.
3. Place the wafer or the sample on the silicon carrier wafer in the centre of the tray. Small pieces must sit on the carrier, never directly on the quartz.
4. Push the tray back in and close the door until the latch clicks.

## Writing a recipe

A recipe is a list of steps: ramp, hold and cool. A typical activation anneal is:

1. Purge with N2 for 60 s at 5 slm.
2. Ramp at 50 °C/s to 400 °C and hold for 10 s (stabilisation).
3. Ramp at 50 °C/s to the target temperature (for example 950 °C) and hold for 30 s.
4. Cool down under N2 flow until the temperature is below 200 °C.

Keep ramp rates at or below 75 °C/s for wafers larger than 100 mm to avoid wafer breakage.

## Running a process

Select your recipe, enter your name in the run log, and click **Start**. Do not leave the tool unattended during the high-temperature steps. If the temperature trace deviates by more than 20 °C from the setpoint, press **Abort**. The tool then switches to N2 purge and cools down automatically.

## Unloading and shutting down

Wait until the chamber is below 100 °C, open the door, remove your samples with the quartz tweezers, and close the door. Leave the N2 purge on standby. Check out on ToolBook.

## Troubleshooting

**The temperature does not reach the setpoint.** The pyrometer window may be coated. Report it to lab staff. Do not increase the lamp power manually.

**A wafer broke in the chamber.** Let it cool, then remove all fragments with the quartz tweezers and report it, because fragments under the tray cause hot spots.
