<!-- tool: Atomic Force Microscope -->
# Atomic Force Microscope: Quick Operating Guide

> **Fictional sample content.** This guide was written for the ToolBook demo. It is not a real instrument manual. Do not use it to operate real equipment.

## Overview

The AFM scans a sharp tip on a flexible cantilever across the sample surface to measure topography with sub-nanometre vertical resolution. The maximum scan size is 90 µm × 90 µm and the maximum vertical range is 8 µm. Use **tapping mode** for most samples, and **contact mode** only for hard, flat samples or when measuring friction.

## Before you start

Check in on ToolBook when you arrive. The AFM is sensitive to vibration and sound: keep the acoustic hood closed during scans, do not lean on the table, and avoid booking it while the cleanroom ventilation maintenance is scheduled.

## Mounting the sample

1. Samples must be smaller than 15 mm × 15 mm and thinner than 5 mm.
2. Fix the sample to a magnetic steel puck with double-sided carbon tape.
3. Place the puck on the scanner stage. The magnet holds it in place.
4. Raise the head with the coarse Z motor before inserting or removing samples, so the tip cannot crash into the surface.

## Installing a cantilever

1. Use tweezers to take a tapping-mode probe (silicon, nominal resonance around 300 kHz) from the probe box.
2. Slide it into the cantilever holder under the spring clip, with the tip facing down.
3. Insert the holder into the head and tighten the locking screw by hand only.
4. Align the laser spot on the end of the cantilever using the two laser adjustment knobs, then centre the reflected spot on the photodetector so the vertical and horizontal deflection signals read close to zero. The SUM signal should be above 2 V.

## Tuning and engaging (tapping mode)

1. Run **Auto Tune** in the software. It finds the cantilever resonance and sets the drive frequency about 5% below the peak.
2. Set the setpoint amplitude to about 80% of the free amplitude.
3. Click **Engage**. The head approaches automatically and stops when the tip reaches the surface.
4. If the engage fails or the tip seems to "fly" above the surface, reduce the setpoint slightly and engage again.

## Scanning

Start with a 5 µm scan at 1 Hz scan rate and 256 × 256 pixels. Watch the trace and retrace lines: they should overlap. If they differ, lower the scan rate or increase the integral gain gradually until the lines match without oscillation. For high-resolution images, reduce the scan size and increase to 512 pixels per line.

## Shutting down

1. Click **Withdraw** and raise the head with the coarse Z motor.
2. Remove the sample and the cantilever holder. Return used probes to the "used" box; never back into the new probe box.
3. Close the software and check out on ToolBook so the actual end time is recorded.

## Troubleshooting

**Streaks or noise in the image.** Usually a contaminated or broken tip. Withdraw, inspect the tip in the optical camera and replace the cantilever if it is damaged. Acoustic noise also causes streaks: close the hood.

**The laser spot cannot be centred on the photodetector.** The cantilever may be mounted crooked, or it is broken. Remount it.
