# Issue #1: Installer Finish Screen Text Truncation / No Scrollbar

## Description
In the final step (Step 5 - Finished Screen) of the **Zenith Suite Installer** (`zenith-setup.exe` compiled from `Installer.cs`), the required integration instructions and status bullet points are partially cut off at the bottom. The user is unable to scroll down to view the remaining installation steps (e.g., how to load the unpacked extension in Google Chrome, select folders, and enable incognito mode).

This issue is caused by using a standard Windows Forms `Label` (`finishDesc`) inside the borderless setup form, which lacks scrollbars or dynamic height adjustment, resulting in hard clipping.

## Visual Context
The problem can be observed in the setup completion screen:
- **Title**: Zenith Suite Configured!
- **Clipping Start**: The text under "Required Actions to complete integration" cuts off during step 2 ("Press Ctrl+V to paste...").
- **Current Layout Limits**: Form size is hardcoded to `520x360`. The content panel height is `200` (located at `Y=60`), giving `finishDesc` a maximum available vertical space of `160`. The label size is hardcoded to `460x140`, starting at `Y=40`.

## Proposed Solution
1. Replace `finishDesc` (currently a `Label`) with a multi-line, read-only `TextBox`.
2. Configure the new `TextBox` to look like a standard label (no border, matching background color, appropriate font).
3. Set `ScrollBars = ScrollBars.Vertical` to allow the user to easily scroll through the remaining instructions if the text exceeds the boundary.
4. Set the `TextBox` dimensions appropriately to maximize the scrollable content area.
5. Recompile `zenith-setup.exe` using `compile.ps1` or run `pack.ps1` to update the distribution ZIP.

## Verification Checklist
- Run `zenith-setup.exe`.
- Proceed through the steps to Step 5 (Finished screen).
- Verify that a vertical scrollbar appears (or mousewheel scrolling works) if the content extends beyond the visible boundary.
- Confirm all instructions (Steps 1, 2, and 3) are completely readable.
- Ensure the aesthetic conforms to the matte-black/gray borderless suite styling guideline (`BackColor` matches the form background `10, 10, 12`).
