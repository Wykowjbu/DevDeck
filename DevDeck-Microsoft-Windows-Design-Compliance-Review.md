# DevDeck — Microsoft Windows App Design Compliance Review

> **Loại tài liệu:** Audit contract + checklist dành cho coding agent  
> **Ứng dụng:** DevDeck  
> **Nền tảng:** Windows 11 Desktop  
> **UI framework:** WinUI 3 / Windows App SDK  
> **Ngày đối chiếu tài liệu Microsoft:** 2026-06-25  
> **Phạm vi:** UI, UX, window behavior, navigation, controls, input, accessibility, theming và Fluent Design  
> **Nguồn chuẩn:** Microsoft Learn — Windows apps design/development documentation

---

## 0. Mục đích và giới hạn của tài liệu

Tài liệu này dùng để yêu cầu coding agent **kiểm tra implementation hiện tại của DevDeck**, không chỉ kiểm tra mockup hoặc cảm giác “giống Windows 11”.

Agent phải đánh giá đồng thời:

1. Source code XAML/C#.
2. Control thực tế đang dùng.
3. Resource/theme/style.
4. Hành vi runtime của cửa sổ.
5. Keyboard, mouse, touch và focus.
6. UI Automation, Narrator và High Contrast.
7. Responsive behavior khi resize và Snap Layout.
8. Trạng thái loading, empty, error, disabled, running và destructive.
9. Terminal nhúng qua WebView2/xterm.js.

### Tuyên bố độ chính xác

Không được tuyên bố “100% compliant” chỉ vì app build thành công hoặc nhìn giống Windows 11. Chỉ được kết luận compliant khi từng mục bắt buộc có bằng chứng source hoặc test runtime.

Microsoft có thể cập nhật guideline sau ngày 2026-06-25. Khi audit ở thời điểm khác, agent phải kiểm tra lại mục **Official Microsoft sources** ở cuối tài liệu và ghi rõ ngày truy cập mới.

### Thứ tự ưu tiên khi có xung đột

1. Tài liệu Microsoft Learn mới nhất dành cho Windows App SDK/WinUI 3.
2. Hành vi mặc định của WinUI 3 common controls và Windows shell.
3. Accessibility và keyboard requirements.
4. Product requirements của DevDeck.
5. Mockup HTML hoặc custom CSS chỉ là tài liệu tham khảo hình ảnh, không phải nguồn hành vi chuẩn.

---

# 1. Prompt bắt buộc gửi cho coding agent

```text
You are reviewing an existing Windows 11 desktop application named DevDeck.
The app is implemented with WinUI 3 and Windows App SDK.

Your job is to perform an evidence-based Microsoft Windows App Design compliance audit.
Do not judge only from screenshots and do not assume that a native-looking UI is compliant.
Inspect all relevant XAML, C#, resources, manifests, windowing code, WebView2 integration,
and runtime behavior.

Use this document as the audit contract.

Mandatory workflow:
1. Inspect the repository structure and identify every UI page, custom control, style,
   resource dictionary, dialog, flyout, navigation surface, title-bar implementation,
   terminal/WebView2 surface, and window-management service.
2. Build and run the application before assigning runtime-related statuses.
3. Audit each checklist item using PASS, FAIL, PARTIAL, NOT TESTED, or N/A.
4. For every PASS, provide evidence such as file path + line/range, runtime test,
   screenshot name, or accessibility tool result.
5. For every FAIL/PARTIAL, explain the Microsoft rule, current implementation,
   user-visible impact, exact remediation, and affected files.
6. Do not mark a runtime or accessibility item PASS from static source inspection alone.
7. Do not replace native WinUI controls with hand-drawn controls unless a documented
   product requirement makes a custom control necessary.
8. Do not change the code during the first audit pass. Produce the report first.
9. After the report, provide a remediation plan ordered by Blocker, Major, Minor, Polish.
10. Do not claim full Microsoft compliance while any mandatory item is FAIL,
    PARTIAL, or NOT TESTED.

Audit target:
- Windows 11 desktop
- WinUI 3 / Windows App SDK
- Custom or extended title bar
- NavigationView-style shell/sidebar
- Workspace and Project collections
- Action buttons with logo + name
- MenuFlyout context actions
- ContentDialog-based editing and confirmations
- Bottom integrated terminal using WebView2/xterm.js and ConPTY
- Light, Dark, System, and High Contrast behavior

Return the final report using the exact format defined near the end of this document.
```

---

# 2. Audit statuses and severity

## 2.1. Status

| Status | Meaning |
|---|---|
| `PASS` | Requirement is satisfied and evidence is supplied. |
| `FAIL` | Requirement is violated or behavior is demonstrably incorrect. |
| `PARTIAL` | Some states, pages, controls, themes, or input methods remain incorrect. |
| `NOT TESTED` | Runtime/accessibility behavior was not verified. This is not a pass. |
| `N/A` | Requirement does not apply; the agent must explain why. |

## 2.2. Severity

| Severity | Meaning |
|---|---|
| `BLOCKER` | Prevents release because of inaccessible core flow, broken window behavior, destructive risk, keyboard trap, unreadable UI, or unusable essential command. |
| `MAJOR` | Clear violation affecting consistency, discoverability, navigation, theme support, input, or major user flow. |
| `MINOR` | Localized inconsistency or polish issue with a workaround. |
| `POLISH` | Optional improvement or context-dependent recommendation. |

## 2.3. Rule strength

| Strength | Meaning |
|---|---|
| `MUST` | Required for this audit; failure blocks a full-compliance claim. |
| `SHOULD` | Strong Microsoft recommendation; deviation requires a documented reason. |
| `MAY` | Contextual option, not a universal requirement. |

---

# 3. Known corrections to the current DevDeck design assumptions

These points must be checked before reviewing smaller details.

## DEV-COR-001 — Do not use a blanket 6–8 px corner radius

**Strength:** MUST  
**Expected Windows 11 geometry:**

- `4 epx`: persistent in-page controls such as Button, CheckBox, ComboBox, TextBox, ListView backplates, progress/scroll/slider bars.
- `8 epx`: top-level containers, app windows, ContentDialog, Flyout, MenuFlyout, TeachingTip.
- `4 epx`: ToolTip.
- `0 epx`: straight edges touching/intersecting neighboring straight edges; snapped or maximized window corners.

Prefer the WinUI resources rather than manually hard-coding every control:

```xaml
{ThemeResource ControlCornerRadius}
{ThemeResource OverlayCornerRadius}
```

Expected defaults:

```text
ControlCornerRadius = 4
OverlayCornerRadius = 8
```

**Audit:** search for custom `CornerRadius="6"`, `CornerRadius="7"`, or global `8` applied to normal buttons/list items and verify each use semantically.

## DEV-COR-002 — Mica should be the primary-window foundation; Acrylic is not generic decoration

**Strength:** MUST/SHOULD depending on surface  

- Mica is appropriate for the app/window base layer and communicates active/inactive window state.
- Acrylic is intended for transient, light-dismiss surfaces such as flyouts and context menus in the Windows design guidance.
- Smoke is used behind modal surfaces such as dialogs.
- Do not apply Acrylic to every panel/card merely to make the interface look premium.
- A user-selectable whole-window “Acrylic” option must be treated as a deliberate alternate backdrop, tested for readability, performance, fallback, inactive state, battery saver/remote sessions, and High Contrast. It must not be assumed to be the canonical default.

## DEV-COR-003 — Light/Dark/System support is insufficient without High Contrast

**Strength:** MUST  

The app must also work in Windows contrast themes. Native common controls help, but custom title bar, custom cards, SVG/image icons, WebView2 terminal, custom backgrounds, and hard-coded colors all require explicit testing.

## DEV-COR-004 — HTML prototype controls are not implementation controls

**Strength:** MUST  

Do not translate prototype elements one-for-one into hand-drawn XAML equivalents when a WinUI control already exists.

Examples:

| Prototype visual | Preferred WinUI implementation |
|---|---|
| Sidebar navigation | `NavigationView` + `Frame` where suitable |
| Context menu | `MenuFlyout`, `MenuFlyoutItem`, `ToggleMenuFlyoutItem`, `MenuFlyoutSubItem` |
| Modal create/edit/delete | `ContentDialog` |
| Binary setting | `ToggleSwitch` or `CheckBox`, chosen by semantics |
| Selection list | `ListView`, `GridView`, `ItemsView`, or `ComboBox` as appropriate |
| Terminal tabs | `TabView` semantics or an accessible equivalent |
| Tooltip | `ToolTipService.ToolTip` |
| Common command bar | `CommandBar`, `AppBarButton`, `XamlUICommand`/`ICommand` where appropriate |

## DEV-COR-005 — “Looks like Windows 11” does not prove native behavior

**Strength:** MUST  

The app must preserve:

- Standard minimize/maximize/close caption buttons.
- Drag regions.
- System menu on title-bar right click.
- Double-click maximize/restore.
- Snap Layout behavior.
- Keyboard focus and activation.
- UI Automation semantics.
- Theme/contrast behavior.

---

# 4. Repository and implementation baseline

## BAS-001 — Confirm native platform

- [ ] **MUST:** The UI project is WinUI 3 using Windows App SDK.
- [ ] **MUST:** The app was not silently replaced with WPF, MAUI, Electron, Avalonia, a browser shell, or a custom Chromium UI.
- [ ] **MUST:** The current package model, target framework, and supported Windows versions are recorded.
- [ ] **MUST:** Windows App SDK version is recorded.
- [ ] **SHOULD:** Agent checks whether the selected SDK version supports the APIs used by the title bar, backdrop, windowing, and controls.

**Evidence required:** `.csproj`, package references, manifest, startup/window code.

## BAS-002 — Inventory all UI surfaces

Agent must produce an inventory containing:

- Windows and secondary windows.
- Pages.
- UserControls/custom controls.
- Resource dictionaries and styles.
- Dialogs, flyouts, menus, tooltips, TeachingTips.
- Navigation containers and Frames.
- List/Grid/Items controls.
- WebView2 surfaces.
- Empty/loading/error states.
- Custom drawing/composition surfaces.

## BAS-003 — Prefer native controls and default behaviors

- [ ] **MUST:** Native controls are used wherever they satisfy the product need.
- [ ] **MUST:** Custom controls have a documented reason.
- [ ] **MUST:** Custom controls implement focus, keyboard activation, pointer states, disabled state, High Contrast, and AutomationPeer support as needed.
- [ ] **SHOULD:** Styling changes preserve the native visual state system instead of replacing the control template unnecessarily.

---

# 5. Window and title-bar compliance

## WIN-001 — Meaningful window identity

- [ ] **MUST:** `Window.Title`/`AppWindow.Title` is a meaningful single-line value such as `DevDeck`, not the template default.
- [ ] **MUST:** App icon is valid at required package sizes and appears correctly in taskbar, Alt+Tab, Start, and title/system surfaces.
- [ ] **SHOULD:** The window title remains meaningful for screen readers and task switching even when custom title text is rendered separately.

## WIN-002 — Standard or custom title bar choice

- [ ] **MUST:** Agent records whether the app uses system title bar, simple customization, extended content, or full customization.
- [ ] **SHOULD:** Use the system title bar or official WinUI/Windows App SDK title-bar APIs rather than simulating caption buttons.
- [ ] **MUST:** System caption controls remain system-managed.
- [ ] **MUST:** Minimize, maximize/restore, and close remain visible and functional in all themes/states.

## WIN-003 — Title-bar geometry

- [ ] **SHOULD:** Standard title bar is approximately `32 epx` high.
- [ ] **SHOULD:** A title bar with interactive content such as global search may use approximately `48 epx` and must account for text scaling.
- [ ] **SHOULD:** Window icon is `16 × 16 epx` and aligned according to Windows guidance.
- [ ] **MUST:** Caption buttons are never clipped or covered.
- [ ] **MUST:** Long title/search/breadcrumb content responds to narrow widths without hiding caption buttons.

## WIN-004 — Drag and system behaviors

Test all of the following:

- [ ] **MUST:** Non-interactive empty title-bar regions drag the window.
- [ ] **MUST:** Interactive controls are excluded from drag regions.
- [ ] **MUST:** Right-click on a non-interactive title-bar region opens the system window menu.
- [ ] **MUST:** Double-click on a non-interactive title-bar region toggles maximize/restore.
- [ ] **MUST:** App icon system-menu behavior is preserved when the icon is exposed.
- [ ] **MUST:** Caption buttons remain clickable and have normal hover/pressed behavior.
- [ ] **MUST:** The title bar differentiates active and inactive window states.

## WIN-005 — Snap Layout and resize

- [ ] **MUST:** Hovering the maximize button can invoke Windows Snap Layouts.
- [ ] **MUST:** Test maximized, restored, minimized, and snapped states.
- [ ] **MUST:** Test 1/2, 1/3, and 1/4 Snap Layout widths where available.
- [ ] **MUST:** App content never renders underneath caption buttons.
- [ ] **MUST:** Window can be resized down to its supported minimum without inaccessible controls or irreversible clipping.
- [ ] **SHOULD:** Minimum size is defined from functional content needs, not merely visual preference.

## WIN-006 — Theme and title bar

- [ ] **MUST:** Title text, icon, caption buttons, hover backgrounds, drag area, and backdrop update correctly for Light, Dark, System, and High Contrast.
- [ ] **MUST:** Theme changes do not require a restart unless a platform limitation is documented.
- [ ] **MUST:** Inactive title-bar state remains legible.
- [ ] **MUST:** Hard-coded title-bar colors do not defeat High Contrast.

## WIN-007 — Mica and fallback

- [ ] **SHOULD:** Mica is used as the default base backdrop when supported and appropriate.
- [ ] **MUST:** A readable solid-color fallback exists when a backdrop is unavailable.
- [ ] **MUST:** Content layers use suitable transparent/theme brushes so Mica is visible without reducing text contrast.
- [ ] **MUST:** Backdrop does not break in inactive state, battery/power constraints, remote desktop, or unsupported systems.

---

# 6. Navigation and information architecture

## NAV-001 — Clear top-level destinations

- [ ] **MUST:** Top-level destinations are clearly named and have a visible selected state.
- [ ] **MUST:** Users can identify their current page and current Workspace/Project context.
- [ ] **MUST:** Labels do not rely only on icons where the meaning is not universally obvious.
- [ ] **SHOULD:** Fewer, important top-level destinations are exposed; secondary destinations are not allowed to overwhelm navigation.

## NAV-002 — Use appropriate navigation pattern

For DevDeck, validate the rationale for:

- `NavigationView`/left navigation for top-level pages.
- List/details for frequent Workspace/Project switching and detail editing.
- `TabView` for dynamically opened terminal sessions.
- `BreadcrumbBar` only when navigation becomes deeper than two meaningful levels.

- [ ] **MUST:** Navigation pattern matches actual hierarchy.
- [ ] **MUST:** A custom sidebar must match NavigationView semantics, focus, keyboard, selected state, compact behavior, and UI Automation—or be replaced by `NavigationView`.
- [ ] **SHOULD:** Left navigation is appropriate when there are several top-level items and page switching is not constant.
- [ ] **SHOULD:** List/details is used where switching among Projects/Actions is frequent.

## NAV-003 — Frame and navigation state

- [ ] **MUST:** Multi-page navigation uses a consistent content host such as `Frame` where appropriate.
- [ ] **MUST:** Navigation does not create duplicate pages/event handlers unintentionally.
- [ ] **MUST:** Selected navigation item and displayed page stay synchronized.
- [ ] **MUST:** Navigation state remains valid after deleting/renaming the selected Workspace or Project.
- [ ] **SHOULD:** Back stack is used only where back navigation is meaningful.

## NAV-004 — Back and Escape behavior

- [ ] **MUST:** `Esc` first dismisses transient UI such as flyouts, menus, and modal/light-dismiss surfaces where expected.
- [ ] **MUST:** Back does not unexpectedly terminate running commands or close the app.
- [ ] **MUST:** Dialog cancellation is predictable and does not save partial edits.
- [ ] **SHOULD:** Back/close affordances are not duplicated without a clear reason.

## NAV-005 — Compact/narrow state

- [ ] **MUST:** Navigation adapts at narrow window widths.
- [ ] **MUST:** Compact navigation still exposes accessible names/tooltips.
- [ ] **MUST:** Main content remains reachable when the sidebar collapses.
- [ ] **MUST:** Terminal panel and navigation do not squeeze the main content into an unusable region.

---

# 7. Responsive layout, spacing, and resizing

## LAY-001 — Design for the app window

- [ ] **MUST:** Responsive behavior is based on current window width, not monitor resolution.
- [ ] **MUST:** Layout works when restored, snapped, and maximized.
- [ ] **MUST:** The app does not assume 1920×1080 or a specific DPI.

## LAY-002 — Microsoft width categories

Use these as design checkpoints, not as the only widths tested:

```text
Small:  up to 640 epx
Medium: 641–1007 epx
Large:  1008 epx and above
```

- [ ] **MUST:** Agent identifies which DevDeck layout decisions change at each relevant breakpoint.
- [ ] **MUST:** Visual states or adaptive triggers do not leave overlapping/hidden content between breakpoints.
- [ ] **SHOULD:** Use responsive techniques such as reflow, reposition, resize, show/hide, or re-architecture rather than uniform shrinking.

## LAY-003 — Four-epx rhythm

- [ ] **SHOULD:** Sizes, margins, padding, and positions generally follow a multiple-of-4 effective-pixel rhythm.
- [ ] **SHOULD:** Deviations are intentional and documented.
- [ ] **MUST:** Repeated components use shared spacing tokens/resources rather than scattered magic numbers.

Recommended audit search:

```text
Margin=
Padding=
Width=
Height=
MinWidth=
MinHeight=
RowSpacing=
ColumnSpacing=
Spacing=
```

## LAY-004 — Content reflow and clipping

- [ ] **MUST:** Text can wrap or trim intentionally; it is not accidentally clipped.
- [ ] **MUST:** Long project paths use an intentional strategy such as trimming + tooltip/copy path.
- [ ] **MUST:** Localized or longer text does not overlap icons, buttons, or adjacent columns.
- [ ] **MUST:** Empty and error messages remain readable at narrow widths.
- [ ] **MUST:** ScrollViewer usage does not create inaccessible nested scrolling.
- [ ] **SHOULD:** Horizontal scrolling is avoided for primary app pages unless the content genuinely requires it.

## LAY-005 — DPI and text scaling

- [ ] **MUST:** Test at Windows display scaling `100%`, `125%`, `150%`, and `200%` where available.
- [ ] **MUST:** Test Windows text-size scaling separately.
- [ ] **MUST:** Fixed heights do not clip scaled text.
- [ ] **MUST:** Title bar, navigation, dialogs, settings rows, action buttons, and terminal controls remain usable.

## LAY-006 — Terminal panel sizing

- [ ] **MUST:** Terminal has a functional minimum and maximum height.
- [ ] **MUST:** Resize grip is keyboard accessible or an equivalent keyboard mechanism exists.
- [ ] **MUST:** Panel resize does not move critical content off-screen without scrolling.
- [ ] **MUST:** Hiding the terminal restores content space without killing terminal processes.
- [ ] **MUST:** Terminal state does not cause layout jumps that hide focus.

---

# 8. Geometry and shape

## GEO-001 — Use WinUI corner resources

- [ ] **MUST:** Standard controls retain WinUI default geometry unless there is a documented product reason.
- [ ] **MUST:** Persistent in-page controls use the `4 epx` family, not a blanket `8 epx`.
- [ ] **MUST:** Flyouts/dialogs/overlays use the `8 epx` family.
- [ ] **MUST:** Adjacent joined controls do not retain rounded touching corners.
- [ ] **MUST:** Maximized/snapped window corners are system-controlled.

## GEO-002 — DevDeck Action buttons

- [ ] **MUST:** Action buttons are actual actionable controls, preferably `Button`, not clickable `Border`, `Grid`, or `StackPanel` with pointer handlers.
- [ ] **MUST:** Default or equivalent hover, pressed, disabled, focus, and keyboard states exist.
- [ ] **SHOULD:** Normal Action button corner radius resolves to `ControlCornerRadius` (`4 epx` by default).
- [ ] **MUST:** Button text and logo remain aligned at every supported density/text scale.
- [ ] **MUST:** Running state is communicated by more than color alone.

## GEO-003 — Cards and list items

- [ ] **MUST:** Cards used as selectable items expose an actual selection/activation semantic.
- [ ] **MUST:** Card shape does not imply a button when only a small nested affordance is actionable.
- [ ] **SHOULD:** Avoid excessive nested rounded rectangles.
- [ ] **SHOULD:** Use borders/layering sparingly and consistently.

---

# 9. Typography

## TYP-001 — Font family

- [ ] **MUST:** UI uses the WinUI/system font behavior, resolving to Segoe UI Variable where supported.
- [ ] **MUST:** Do not package or redistribute Microsoft font files.
- [ ] **SHOULD:** Use one UI font throughout the app.
- [ ] **MAY:** Terminal uses an appropriate monospace font such as Cascadia Mono or Consolas.

## TYP-002 — Windows type ramp

Validate use of built-in text styles/resources rather than arbitrary font sizes.

Reference values:

| Role | Weight | Size / line height |
|---|---:|---:|
| Caption/small | Regular | 12 / 16 epx |
| Body | Regular | 14 / 20 epx |
| Body strong | Semibold | 14 / 20 epx |
| Body large | Regular | 18 / 24 epx |
| Body large strong | Semibold | 18 / 24 epx |
| Subtitle | Semibold | 20 / 28 epx |
| Title | Semibold | 28 / 36 epx |
| Title large | Semibold | 40 / 52 epx |
| Display | Semibold | 68 / 92 epx |

- [ ] **MUST:** Avoid UI text smaller than `12 epx Regular` or `14 epx Semibold` without a strong, tested reason.
- [ ] **SHOULD:** Use `Regular` for most text and `Semibold` for titles/emphasis.
- [ ] **SHOULD:** Avoid Bold and Italic as primary hierarchy tools.
- [ ] **SHOULD:** Use sentence case for UI labels and titles.

## TYP-003 — Alignment and readability

- [ ] **SHOULD:** Text is left-aligned by default.
- [ ] **SHOULD:** Center alignment is limited to appropriate short empty states/icon captions.
- [ ] **MUST:** Paragraph line length is readable; long instructional text is not stretched across the full window.
- [ ] **MUST:** Truncation has a deliberate strategy and does not hide essential distinctions.
- [ ] **MUST:** Full project path/action command remains discoverable via tooltip, details, copy, or expansion where trimmed.

## TYP-004 — Terminal typography

- [ ] **MUST:** Terminal font is monospaced and user-configurable only within safe ranges.
- [ ] **MUST:** Terminal text remains readable at default size and under display/text scaling.
- [ ] **MUST:** Terminal does not hard-code a font unavailable on the target system without fallback.
- [ ] **MUST:** Line height does not clip glyphs, Vietnamese diacritics, CJK, emoji, or powerline symbols used by shells.

---

# 10. Color, theme, contrast, and resources

## COL-001 — Follow user theme by default

- [ ] **MUST:** `System` theme follows Windows settings.
- [ ] **MUST:** App does not hard-code `RequestedTheme` when the selected setting is System.
- [ ] **MUST:** Explicit Light/Dark selection updates the root content consistently.
- [ ] **MUST:** Theme preference persists only as intended.
- [ ] **MUST:** All open dialogs/flyouts/title bar/terminal chrome update or reopen correctly after theme changes.

## COL-002 — Theme resources over hard-coded colors

- [ ] **MUST:** Reusable semantic brushes are defined in theme dictionaries/resources.
- [ ] **MUST:** Custom templates use `{ThemeResource ...}` rather than hard-coded light-only/dark-only colors.
- [ ] **MUST:** Hard-coded colors are limited to intentional brand/status colors and have theme/contrast alternatives.
- [ ] **MUST:** No foreground/background pair becomes unreadable in any supported theme.

Audit searches:

```text
Foreground="#
Background="#
Color="#
Colors.
Color.FromArgb
SolidColorBrush
RequestedTheme=
```

## COL-003 — Accent color

- [ ] **SHOULD:** Respect the Windows system accent color by default.
- [ ] **MUST:** Accent is used sparingly for meaningful emphasis/state.
- [ ] **MUST:** Accent is not used for large decorative backgrounds that reduce readability.
- [ ] **MUST:** Custom accent text/background combinations are contrast-tested in both Light and Dark.
- [ ] **MUST:** Selected/focus/running/error states are not communicated only through an accent hue.

## COL-004 — Contrast

- [ ] **MUST:** Normal text contrast is at least `4.5:1`.
- [ ] **MUST:** Large text contrast is at least `3:1`.
- [ ] **MUST:** Focus indicators, component boundaries, and meaningful non-text graphics remain perceptible.
- [ ] **MUST:** Disabled states remain understandable without being mistaken for enabled controls.
- [ ] **MUST:** Contrast is measured, not estimated visually.

## COL-005 — High Contrast

- [ ] **MUST:** Test at least one Windows contrast theme end-to-end.
- [ ] **MUST:** Custom foreground/background colors yield to appropriate system/theme resources.
- [ ] **MUST:** Selected, hover, pressed, focus, disabled, error, warning, success, and running states remain distinguishable.
- [ ] **MUST:** SVG/PNG icons remain visible or receive a high-contrast alternative.
- [ ] **MUST:** Custom title bar and terminal chrome remain operable.
- [ ] **MUST:** No information is encoded solely as red/green or hue.

## COL-006 — Terminal color schemes

- [ ] **MUST:** Terminal default scheme has readable foreground/background contrast.
- [ ] **MUST:** ANSI colors are tested on the selected terminal background.
- [ ] **MUST:** Error/success/warning output is not distinguishable by color alone when DevDeck adds its own status UI.
- [ ] **MUST:** xterm.js selection, cursor, links, focus, and search results remain visible.
- [ ] **MUST:** High Contrast behavior is explicitly documented; a fallback terminal theme is supplied when automatic adaptation is insufficient.

---

# 11. Materials, layering, and elevation

## MAT-001 — Correct material by purpose

- [ ] **SHOULD:** Mica is used as the main window foundation where supported.
- [ ] **SHOULD:** Acrylic is limited to appropriate transient/light-dismiss surfaces or a deliberately tested backdrop mode.
- [ ] **MUST:** Smoke/dimming appears behind blocking modal dialogs through native behavior.
- [ ] **MUST:** Materials do not obscure text or controls.
- [ ] **MUST:** Every material has a theme-aware solid fallback.

## MAT-002 — Layer hierarchy

- [ ] **MUST:** Base, content, transient, and modal layers are visually distinguishable.
- [ ] **SHOULD:** Windows 11’s restrained layering is followed; do not apply shadow to every card/control.
- [ ] **MUST:** Flyouts/dialogs appear above the correct owner and do not render behind WebView2.
- [ ] **MUST:** Modal state blocks background interaction and focus.

## MAT-003 — WebView2 airspace/z-order

- [ ] **MUST:** ContentDialog, flyout, context menu, tooltip, and resize affordances work correctly around the terminal WebView2.
- [ ] **MUST:** WebView2 does not visually cover native overlays.
- [ ] **MUST:** Focus moves correctly between native XAML and WebView2.
- [ ] **MUST:** Hidden terminal WebView2 does not continue intercepting pointer/keyboard input.

## MAT-004 — Performance and power

- [ ] **MUST:** Backdrop, blur, shadow, and animation do not cause obvious resize/scroll jank.
- [ ] **MUST:** Effects degrade gracefully when transparency is disabled or unsupported.
- [ ] **SHOULD:** Avoid unnecessary composition layers and repeated expensive effects.

---

# 12. Iconography

## ICO-001 — System command icons

- [ ] **SHOULD:** Use Segoe Fluent Icons or WinUI `SymbolIcon`/`FontIcon` for standard commands such as Add, Delete, Edit, Settings, Search, More, Back, Play, Stop, Folder.
- [ ] **MUST:** Do not use legacy/incorrect glyphs where a current Fluent symbol exists.
- [ ] **MUST:** Icon meaning matches Windows conventions.
- [ ] **MUST:** Destructive icons are not used decoratively.

## ICO-002 — Size and alignment

- [ ] **SHOULD:** Standard command icons use appropriate optical sizes such as `16`, `20`, or `24 epx` based on the control.
- [ ] **MUST:** Icon alignment and baseline are consistent.
- [ ] **MUST:** Icon-only buttons still have a sufficiently large hit target.
- [ ] **MUST:** Icons do not stretch, blur, or become clipped under scaling.

## ICO-003 — Product/tool logos

DevDeck legitimately displays third-party tool logos on Action buttons.

- [ ] **MUST:** Each logo has an accessible Action name; the logo alone is not the label.
- [ ] **MUST:** Missing/corrupt icon has a deterministic fallback.
- [ ] **MUST:** Imported logos are copied safely into app storage as required by the product design.
- [ ] **MUST:** Light/dark/contrast visibility is tested.
- [ ] **SHOULD:** Logos are not recolored in a way that damages brand recognition unless a monochrome variant is intended.
- [ ] **MUST:** No icon file path can escape approved storage or execute content.

## ICO-004 — Icon-only controls

- [ ] **MUST:** Every icon-only control has `AutomationProperties.Name` or an equivalent accessible name.
- [ ] **MUST:** A tooltip explains the action.
- [ ] **MUST:** Hover, pressed, focus, disabled, and selected states are visible.
- [ ] **MUST:** The icon is not the only signal for a destructive or stateful action.

---

# 13. Controls, commanding, menus, and dialogs

## CMD-001 — Choose the correct control

- [ ] **MUST:** `Button` for immediate action.
- [ ] **MUST:** `ToggleButton`/`ToggleSwitch` only for persistent binary state where semantics fit.
- [ ] **MUST:** `CheckBox` for independent choices; `RadioButtons` for mutually exclusive visible choices; `ComboBox` for compact choice lists.
- [ ] **MUST:** Text inputs use appropriate `TextBox`, `NumberBox`, `PasswordBox`, `AutoSuggestBox`, etc.
- [ ] **MUST:** Do not use clickable text, Border, Grid, or StackPanel as a substitute for a button without full semantics.

## CMD-002 — Command placement

- [ ] **MUST:** Primary/common actions are visible near the relevant content.
- [ ] **SHOULD:** Less-frequent actions move to overflow/context menus.
- [ ] **MUST:** Destructive actions are separated and clearly labeled.
- [ ] **MUST:** Disabled commands explain prerequisites when otherwise confusing.
- [ ] **SHOULD:** Shared commands use `ICommand`, `XamlUICommand`, or equivalent command binding where beneficial.

## CMD-003 — Action execution feedback

- [ ] **MUST:** Clicking an Action gives immediate visual feedback.
- [ ] **MUST:** Running, succeeded, failed, cancelled, and unavailable states are distinguishable.
- [ ] **MUST:** Repeated clicking cannot accidentally launch duplicate dangerous processes unless explicitly supported.
- [ ] **MUST:** Long-running Action can be stopped when technically applicable.
- [ ] **MUST:** Status is exposed to UI Automation/live region where appropriate.
- [ ] **SHOULD:** Progress indicators are determinate when progress is known and indeterminate otherwise.

## CMD-004 — Context menus

- [ ] **MUST:** Use `MenuFlyout` family controls for native context menus.
- [ ] **MUST:** Right-click is not the only way to access essential commands.
- [ ] **MUST:** Keyboard access (`Shift+F10`/Menu key where supported) exposes equivalent commands.
- [ ] **MUST:** Menu item labels are explicit; icons are supplemental.
- [ ] **MUST:** Menu closes with Escape and returns focus correctly.
- [ ] **MUST:** Menu commands apply to the item that invoked the menu, not stale selection.

## CMD-005 — Dialogs

- [ ] **MUST:** Use `ContentDialog` or another appropriate native dialog pattern.
- [ ] **MUST:** Primary, secondary, and close actions have clear labels.
- [ ] **MUST:** Default and cancel behavior are correct.
- [ ] **MUST:** Validation errors keep the dialog open and move/announce focus appropriately.
- [ ] **MUST:** Dialog content remains usable with text scaling and at narrow widths.
- [ ] **MUST:** Opening/closing returns focus predictably.
- [ ] **MUST:** Only one modal dialog is active per XamlRoot.

## CMD-006 — Confirmation and undo

- [ ] **MUST:** Confirm irreversible or high-impact actions.
- [ ] **SHOULD:** Do not show confirmation for routine reversible actions; prefer undo when feasible.
- [ ] **MUST:** Confirmation text names the object and consequence.
- [ ] **MUST:** Removing a Project/Workspace never implies deleting its real folder unless the product explicitly adds that separately.
- [ ] **MUST:** Dangerous command execution honors the product’s confirmation setting.

## CMD-007 — Settings controls

- [ ] **MUST:** A single discoverable Settings entry point exists.
- [ ] **MUST:** Settings are grouped by user intent, not internal implementation class.
- [ ] **MUST:** Every setting label explains the effect; risky settings include consequence/help text.
- [ ] **MUST:** Toggle state and selected values persist correctly.
- [ ] **MUST:** Reset restores documented defaults.
- [ ] **SHOULD:** Settings that take effect immediately do so consistently; otherwise say when they take effect.

---

# 14. Input, keyboard, pointer, touch, and focus

## INP-001 — Keyboard-only operation

Every core flow must be completable without a mouse:

- [ ] **MUST:** Create/rename/switch/delete Workspace.
- [ ] **MUST:** Add/open/remove Project.
- [ ] **MUST:** Create/edit/assign/run/stop Action.
- [ ] **MUST:** Open context commands through an accessible alternative.
- [ ] **MUST:** Open, resize/hide, switch, and close terminal tabs.
- [ ] **MUST:** Navigate Settings and change values.
- [ ] **MUST:** Dismiss menus/dialogs and recover focus.

## INP-002 — Tab order

- [ ] **MUST:** Every actionable item is a tab stop when appropriate.
- [ ] **MUST:** Non-actionable decoration is not a tab stop.
- [ ] **MUST:** Tab order follows visual/logical reading order.
- [ ] **MUST:** Hidden/collapsed controls are not reachable.
- [ ] **MUST:** Initial focus is placed logically on page/dialog activation.
- [ ] **MUST:** No keyboard trap exists in native UI or WebView2 terminal.

## INP-003 — Arrow-key navigation

- [ ] **MUST:** Composite controls use expected arrow-key navigation.
- [ ] **MUST:** NavigationView/ListView/TabView behavior is preserved instead of reimplemented incorrectly.
- [ ] **MUST:** Arrow keys inside the terminal continue to reach the shell when terminal has focus.
- [ ] **MUST:** App-level shortcuts do not steal common terminal keystrokes unexpectedly.

## INP-004 — Enter, Space, Escape, and context key

- [ ] **MUST:** Focused Buttons activate with expected keys.
- [ ] **MUST:** Space does not scroll when it should activate a focused button.
- [ ] **MUST:** Escape closes the topmost dismissible surface before affecting the page/window.
- [ ] **MUST:** Context Menu key or `Shift+F10` provides relevant context commands where supported.
- [ ] **MUST:** Delete key is not destructive without context/confirmation safeguards.

## INP-005 — Keyboard accelerators and access keys

- [ ] **SHOULD:** Common, frequent commands have discoverable keyboard accelerators.
- [ ] **MUST:** Accelerator conflicts are checked, especially with shell/terminal shortcuts.
- [ ] **MUST:** Shortcuts are shown in menus/tooltips/help where users can discover them.
- [ ] **MUST:** Shortcuts respect enabled/disabled command state.
- [ ] **SHOULD:** Use standard conventions where appropriate, for example Ctrl+F for search and Ctrl+, for settings only if consistent with the product.

## INP-006 — Focus visuals

- [ ] **MUST:** Keyboard focus is always visible.
- [ ] **MUST:** Focus indicator has sufficient contrast in Light, Dark, and High Contrast.
- [ ] **MUST:** Custom styles do not remove default focus visuals without an equivalent replacement.
- [ ] **MUST:** Focus is not hidden behind the terminal panel, flyout, or viewport edge.
- [ ] **MUST:** After deletion/navigation/dialog close, focus lands on a logical surviving element.

## INP-007 — Pointer states

- [ ] **MUST:** Hover, pressed, selected, disabled, drag, and context-menu states are visibly distinct.
- [ ] **MUST:** Cursor changes only where meaningful.
- [ ] **MUST:** Right-click does not trigger left-click primary action.
- [ ] **MUST:** Double-click behavior is not assigned unpredictably to normal action controls.

## INP-008 — Touch targets

- [ ] **MUST:** Interactive target generally provides approximately `40 × 40 epx` hit area.
- [ ] **MUST:** Icon-only title/terminal controls are not reduced to the visible glyph size.
- [ ] **MUST:** Frequently used or dangerous controls receive adequate spacing.
- [ ] **MUST:** Dense mode may reduce visuals but must not make hit targets unusable or inaccessible.

## INP-009 — Drag and drop, if implemented

- [ ] **MUST:** Drag/drop has keyboard-accessible alternatives.
- [ ] **MUST:** Drop target and insertion position are visible.
- [ ] **MUST:** Invalid drops are rejected with feedback.
- [ ] **MUST:** Reordering persists correctly and does not lose selection/focus.

---

# 15. Accessibility and UI Automation

## A11Y-001 — Accessible names

- [ ] **MUST:** Every interactive element has an accessible name.
- [ ] **MUST:** Icon-only controls have explicit names.
- [ ] **MUST:** Images/logos that convey meaning have names; decorative images are excluded appropriately.
- [ ] **MUST:** Names describe the action, not the control type alone—for example `Stop terminal process`, not `Button`.
- [ ] **MUST:** Dynamic names update when context changes.

## A11Y-002 — Roles, states, values, and patterns

- [ ] **MUST:** UI Automation control type matches semantics.
- [ ] **MUST:** Selected, expanded/collapsed, checked, running, unavailable, and progress states are exposed.
- [ ] **MUST:** Custom controls implement suitable AutomationPeer and control patterns.
- [ ] **MUST:** A clickable card is not announced as generic text/group.
- [ ] **MUST:** Terminal tabs expose name, selected state, close action, and position where feasible.

## A11Y-003 — Form labels and errors

- [ ] **MUST:** Every field has a visible label.
- [ ] **MUST:** Labels are associated through native Header/LabeledBy behavior where applicable.
- [ ] **MUST:** Placeholder text is not the only label.
- [ ] **MUST:** Required state is conveyed programmatically and visually.
- [ ] **MUST:** Validation message identifies the field and remedy.
- [ ] **MUST:** Error indication does not depend only on red color.

## A11Y-004 — Screen reader reading order

- [ ] **MUST:** Narrator order matches visual/logical order.
- [ ] **MUST:** Page title/current context is announced meaningfully.
- [ ] **MUST:** Decorative elements do not create noise.
- [ ] **MUST:** Repeated Action lists expose concise item names and state.
- [ ] **MUST:** Opening a dialog/flyout changes screen-reader context correctly.

## A11Y-005 — Dynamic updates

- [ ] **MUST:** Important Action completion/failure is announced without forcing focus.
- [ ] **MUST:** Error banners/toasts are exposed to assistive technology.
- [ ] **MUST:** Rapid terminal output does not flood Narrator uncontrollably.
- [ ] **SHOULD:** DevDeck’s summarized status UI announces meaningful state while raw terminal output remains user-controlled.

## A11Y-006 — Magnifier and zoom

- [ ] **MUST:** Essential controls remain discoverable with Magnifier.
- [ ] **MUST:** Focus movement scrolls the focused control into view.
- [ ] **MUST:** Dialogs and Settings do not rely on fixed viewport assumptions.

## A11Y-007 — Accessibility test tools

Run and report:

- [ ] **MUST:** Accessibility Insights for Windows — FastPass.
- [ ] **MUST:** Accessibility Insights/Inspect inspection for representative controls.
- [ ] **MUST:** Narrator keyboard-only smoke test.
- [ ] **MUST:** High Contrast end-to-end smoke test.
- [ ] **MUST:** Contrast measurements for custom colors.
- [ ] **SHOULD:** Add automated accessibility validation to CI where practical.

Do not mark the accessibility section PASS if a tool was not run.

---

# 16. Motion and animation

## MOT-001 — Purposeful motion

- [ ] **SHOULD:** Motion communicates state change, continuity, hierarchy, or direct feedback.
- [ ] **MUST:** Animation is not added merely to imitate a web dashboard.
- [ ] **MUST:** Motion does not delay command execution or input response.
- [ ] **MUST:** Repeated hover animation is subtle and stable.
- [ ] **MUST:** Animations do not cause layout shifts that move the user’s target.

## MOT-002 — Native transitions

- [ ] **SHOULD:** Prefer WinUI theme transitions/connected animations where they fit.
- [ ] **MUST:** Navigation transition direction matches the navigation relationship.
- [ ] **MUST:** Dialog/flyout animation is not replaced by conflicting custom animation.

## MOT-003 — Reduced motion and performance

- [ ] **MUST:** App remains usable when animations are disabled/reduced by system or app setting.
- [ ] **MUST:** No essential information is available only during animation.
- [ ] **MUST:** Terminal output/resize remains responsive while other animation runs.

---

# 17. Content, labels, errors, and writing style

## TXT-001 — Voice and clarity

- [ ] **MUST:** Labels are concise, clear, and action-oriented.
- [ ] **MUST:** Use familiar terms consistently: Workspace, Project, Action, Step, Terminal.
- [ ] **MUST:** Do not alternate between Command/Workflow/Action when the product model defines Action.
- [ ] **SHOULD:** Lead with the most important information.
- [ ] **SHOULD:** Avoid blaming the user.

## TXT-002 — Button and menu labels

- [ ] **MUST:** Buttons state the action, for example `Create workspace`, `Save changes`, `Remove project`.
- [ ] **MUST:** Avoid ambiguous labels such as `OK` when a specific verb is possible.
- [ ] **MUST:** Ellipsis is used only when a command requires additional user input before completion.
- [ ] **MUST:** Destructive labels name the destructive action.

## TXT-003 — Error messages

Every error should answer:

1. What happened?
2. What was affected?
3. What can the user do next?

- [ ] **MUST:** Error contains useful context without exposing secrets.
- [ ] **MUST:** File/path/process errors provide a safe actionable next step.
- [ ] **MUST:** Shell command output is not replaced by a vague generic error.
- [ ] **MUST:** Copy details/log option exists for complex failures when appropriate.
- [ ] **MUST:** Error wording distinguishes validation failure, process exit failure, permission denial, missing executable, missing folder, and cancellation.

## TXT-004 — Destructive confirmation copy

- [ ] **MUST:** Confirmation explicitly states that removing a Project/Workspace from DevDeck does not delete the real folder.
- [ ] **MUST:** Deleting a global Action explains effects on assignments/overrides.
- [ ] **MUST:** Killing a process is distinguished from hiding/closing the terminal panel.

## TXT-005 — Localization readiness

- [ ] **SHOULD:** User-visible strings are not scattered through code-behind.
- [ ] **MUST:** Layout tolerates longer localized strings.
- [ ] **MUST:** File paths, command text, and user-entered names are not transformed incorrectly.
- [ ] **SHOULD:** FlowDirection behavior is not irreparably hard-coded even if RTL is outside MVP.

---

# 18. DevDeck-specific review

## DEV-001 — Action button `[Logo + Action Name]`

- [ ] **MUST:** Implemented as a semantic actionable control.
- [ ] **MUST:** Approximately `40–44 epx` visual height is acceptable only when the full hit target remains at least about `40 × 40 epx`.
- [ ] **SHOULD:** Corner radius uses `ControlCornerRadius` rather than custom `6–8`.
- [ ] **MUST:** Logo is supplemental; Action name remains accessible.
- [ ] **MUST:** Enter/Space activates the Action.
- [ ] **MUST:** Right-click opens native `MenuFlyout` without also running the Action.
- [ ] **MUST:** Equivalent context commands are available by keyboard.
- [ ] **MUST:** Running/failed/succeeded/disabled states do not depend on color alone.
- [ ] **MUST:** Long names wrap/trim intentionally without overlapping.
- [ ] **MUST:** Pointer-over/pressed/focus states are native or equivalent.

## DEV-002 — Workspace and Project selection

- [ ] **MUST:** Selection state is programmatic and visible.
- [ ] **MUST:** Current Workspace and Project are always clear.
- [ ] **MUST:** Selection is preserved or reset logically after data refresh/delete.
- [ ] **MUST:** Cards/list items are keyboard navigable.
- [ ] **MUST:** Path is readable/copyable but does not dominate the item.
- [ ] **MUST:** Empty state offers the primary next action.

## DEV-003 — Global Action versus Project override

- [ ] **MUST:** UI clearly labels whether editing is global or Project-specific.
- [ ] **MUST:** Dialog title, body, and save action identify the scope.
- [ ] **MUST:** Reset-to-global is discoverable and safe.
- [ ] **MUST:** Inherited versus overridden values are visually and programmatically understandable.
- [ ] **MUST:** Context menu invoked on one Action never edits another due to stale data context.

## DEV-004 — Dangerous Actions

- [ ] **MUST:** Commands are never auto-run when merely opening/selecting a Project.
- [ ] **MUST:** Dangerous Action confirmation is explicit and scoped to the effective command.
- [ ] **MUST:** Confirmation cannot be bypassed accidentally by rapid repeated input.
- [ ] **MUST:** Passwords/tokens are not displayed in confirmation, logs, or accessible names.
- [ ] **MUST:** Process elevation behavior is clear and not silently triggered.

## DEV-005 — Integrated terminal shell

- [ ] **MUST:** Terminal is initialized lazily when required, unless a measured product reason says otherwise.
- [ ] **MUST:** Focus can enter the terminal by keyboard.
- [ ] **MUST:** Focus can leave the terminal without a mouse; no WebView2 keyboard trap.
- [ ] **MUST:** Shell receives expected editing/navigation/control keys.
- [ ] **MUST:** App-level accelerators do not intercept shell keys unexpectedly.
- [ ] **MUST:** Terminal input/output supports Unicode and Vietnamese text.
- [ ] **MUST:** Copy/paste behavior is documented and does not conflict with shell interrupt semantics.
- [ ] **MUST:** Terminal link activation is safe and intentional.
- [ ] **MUST:** Terminal context menu is accessible or equivalent commands exist.

## DEV-006 — Terminal tabs

- [ ] **MUST:** Multiple sessions are represented with accessible tab semantics.
- [ ] **MUST:** Active tab is visually and programmatically selected.
- [ ] **MUST:** Each tab has a useful unique name, not only `Terminal 1` when project/action context is available.
- [ ] **MUST:** Tab switching works by keyboard.
- [ ] **MUST:** Close tab and kill process are distinct commands.
- [ ] **MUST:** Closing a running tab prompts or follows a documented safe policy.
- [ ] **MUST:** Closing one tab does not kill unrelated sessions.
- [ ] **MUST:** Tab overflow remains navigable.

## DEV-007 — Hide, close, and kill semantics

Validate these as separate operations:

```text
Hide terminal panel ≠ Close terminal session ≠ Kill process tree
```

- [ ] **MUST:** Hiding the panel keeps sessions/processes alive.
- [ ] **MUST:** Closing a tab disposes the terminal session according to the selected policy.
- [ ] **MUST:** Kill terminates the intended process tree and reports outcome.
- [ ] **MUST:** UI labels do not conflate these operations.
- [ ] **MUST:** Narrator/accessibility names distinguish these operations.
- [ ] **MUST:** App shutdown with running processes presents a clear policy/confirmation.

## DEV-008 — WebView2 terminal accessibility

- [ ] **MUST:** Agent tests UI Automation/Narrator against the actual terminal surface.
- [ ] **MUST:** Native controls surrounding WebView2 remain reachable in logical order.
- [ ] **MUST:** Terminal cursor/selection is visible in Light/Dark and High Contrast fallback.
- [ ] **MUST:** Browser default context/accessibility behavior is not disabled without replacement.
- [ ] **MUST:** Zoom/text sizing strategy is documented.
- [ ] **MUST:** Web content receives correct theme notification.
- [ ] **MUST:** Security/navigation restrictions prevent terminal content from navigating the WebView to arbitrary remote pages unless intentionally allowed.

## DEV-009 — Settings page

- [ ] **MUST:** Theme uses a suitable choice control with System/Light/Dark.
- [ ] **MUST:** High Contrast is not presented as a normal app theme that overrides Windows; the app follows Windows contrast mode.
- [ ] **MUST:** Backdrop choices explain compatibility/fallback.
- [ ] **MUST:** Density/Action size options preserve target size, focus, text scaling, and accessibility.
- [ ] **MUST:** Terminal font/size choices show valid ranges and fallback behavior.
- [ ] **MUST:** Reset application is separated, destructive, and confirmed.
- [ ] **MUST:** Import/export provides validation and clear success/failure feedback.

## DEV-010 — Notifications and transient feedback

- [ ] **MUST:** Completion feedback does not steal focus unnecessarily.
- [ ] **MUST:** Important failures remain discoverable after a transient notification disappears.
- [ ] **MUST:** Toast/in-app notification action labels are clear.
- [ ] **MUST:** Notification is not the only place to learn whether an Action failed.

## DEV-011 — File/folder pickers

- [ ] **MUST:** Folder Picker is invoked with the correct WinUI desktop HWND initialization.
- [ ] **MUST:** Cancellation is handled as a normal outcome.
- [ ] **MUST:** Invalid/deleted/inaccessible project paths produce an actionable state.
- [ ] **MUST:** Removing a Project record never deletes the real folder.
- [ ] **MUST:** Open Folder/Open File uses safe platform behavior and reports failure.

## DEV-012 — Data safety and destructive UI

- [ ] **MUST:** Workspace delete does not delete real project folders.
- [ ] **MUST:** Project remove does not delete the folder.
- [ ] **MUST:** Removing an assigned Action does not delete the global Action.
- [ ] **MUST:** Deleting a global Action clearly explains assignment/override impact.
- [ ] **MUST:** Imported icon cleanup cannot delete arbitrary user files.
- [ ] **MUST:** UI copy exactly matches actual data behavior.

---

# 19. Required runtime test matrix

Agent must record OS build, Windows App SDK version, device scale, and app version/commit.

## 19.1. Window width and Snap tests

| Test | Required result |
|---|---|
| Restored large window ≥1008 epx | Full navigation/content/terminal usable. |
| Medium 641–1007 epx | Layout adapts without overlap or inaccessible commands. |
| Small ≤640 epx, if supported | Navigation/content reflow or documented minimum width. |
| Snap 1/2 | Core flow usable. |
| Snap 1/3 | Core flow usable or supported minimum behavior documented. |
| Snap 1/4 | Core flow usable or supported minimum behavior documented. |
| Maximized | No custom corner/caption issue. |
| Inactive window | Mica/title text/caption state clear. |

## 19.2. Theme tests

| Theme/state | Required result |
|---|---|
| System → Light | All native/custom/WebView2 surfaces update correctly. |
| System → Dark | All native/custom/WebView2 surfaces update correctly. |
| Explicit Light | Persists and remains legible. |
| Explicit Dark | Persists and remains legible. |
| Windows contrast theme | Core flow fully operable; no lost information. |
| Transparency disabled/fallback | App remains readable. |

## 19.3. Scaling tests

| Test | Required result |
|---|---|
| Display scaling 100% | Baseline. |
| Display scaling 125% | No clipping/blur. |
| Display scaling 150% | No clipping/blur. |
| Display scaling 200% | Core flow remains usable. |
| Increased Windows text size | Text and controls reflow; no fixed-height clipping. |

## 19.4. Input tests

| Input | Required result |
|---|---|
| Keyboard only | Every core flow completes. |
| Mouse | Hover/pressed/context/drag behavior correct. |
| Touch, when hardware available | Targets and scrolling usable. |
| Narrator + keyboard | Names, states, order, dialogs, status understandable. |
| Magnifier | Focus and critical controls remain discoverable. |

## 19.5. State tests

Test each representative control/page in:

```text
Default
Pointer over
Pressed
Focused
Selected
Disabled
Loading
Empty
Error
Running
Succeeded
Failed
Cancelled
High Contrast
Narrow window
Increased text size
```

---

# 20. Static source searches the agent must perform

These searches are not proof by themselves; they locate review hotspots.

```text
CornerRadius=
ControlCornerRadius
OverlayCornerRadius
Background="#
Foreground="#
Color="#
Colors.
Color.FromArgb
RequestedTheme=
FontFamily=
FontSize=
FontWeight="Bold"
FontStyle="Italic"
Width=
Height=
MinWidth=
MinHeight=
Margin=
Padding=
PointerPressed=
Tapped=
RightTapped=
DoubleTapped=
IsTabStop=
TabIndex=
KeyboardAccelerator
AccessKey=
AutomationProperties.Name
AutomationProperties.HelpText
AutomationProperties.LabeledBy
AutomationPeer
SetTitleBar
ExtendsContentIntoTitleBar
InputNonClientPointerSource
AppWindowTitleBar
SystemBackdrop
MicaBackdrop
DesktopAcrylicBackdrop
WebView2
CoreWebView2
ContentDialog
MenuFlyout
Flyout
Border
Grid
StackPanel
```

Agent must flag clickable `Border`, `Grid`, `StackPanel`, `TextBlock`, or `Image` implementations for semantic review.

---

# 21. Required audit report format

```markdown
# DevDeck Microsoft Windows Design Compliance Audit

## 1. Audit metadata
- Repository/branch:
- Commit:
- App version:
- Windows version/build:
- Windows App SDK version:
- Audit date:
- Auditor/agent:
- Build result:

## 2. Executive conclusion
- Overall status: PASS / NOT COMPLIANT / INCOMPLETE AUDIT
- Release blockers:
- Major findings:
- Untested areas:
- Important note: do not provide a percentage as a substitute for unresolved findings.

## 3. Evidence inventory
| Surface | Implementation | Main files | Runtime tested |
|---|---|---|---|

## 4. Compliance summary
| Section | PASS | FAIL | PARTIAL | NOT TESTED | N/A |
|---|---:|---:|---:|---:|---:|
| Baseline | | | | | |
| Window/title bar | | | | | |
| Navigation | | | | | |
| Layout | | | | | |
| Geometry | | | | | |
| Typography | | | | | |
| Color/theme | | | | | |
| Materials | | | | | |
| Iconography | | | | | |
| Controls/commands | | | | | |
| Input/keyboard | | | | | |
| Accessibility | | | | | |
| Motion | | | | | |
| Writing/content | | | | | |
| DevDeck-specific | | | | | |

## 5. Findings
| ID | Severity | Status | Guideline | Current evidence | User impact | Required fix | Files |
|---|---|---|---|---|---|---|---|

## 6. Runtime test results
| Test | Result | Evidence | Notes |
|---|---|---|---|

## 7. Accessibility tool results
- Accessibility Insights FastPass:
- Inspect/UI Automation:
- Narrator:
- High Contrast:
- Contrast measurements:
- Keyboard-only flow:

## 8. Likely violations found in source
### BLOCKER
- ...

### MAJOR
- ...

### MINOR
- ...

### POLISH
- ...

## 9. Remediation plan
| Order | Finding IDs | Change | Files | Verification |
|---:|---|---|---|---|

## 10. Final declaration
- Mandatory FAIL count:
- Mandatory PARTIAL count:
- Mandatory NOT TESTED count:
- Can the app be described as Microsoft-guideline compliant? Yes/No
- Reason:
```

---

# 22. Definition of done for the audit

The audit itself is complete only when:

1. The repository and relevant UI files were inspected.
2. The app was built and launched.
3. Every checklist section has statuses.
4. Runtime-only items are not marked PASS from code alone.
5. Custom title bar was tested for drag, system menu, maximize/restore, caption buttons, Snap Layout, themes, and scaling.
6. Core flows were completed keyboard-only.
7. Accessibility Insights, Narrator, contrast, and High Contrast tests were recorded.
8. Light, Dark, System, and Windows contrast modes were tested.
9. Representative breakpoint, Snap, DPI, and text-scaling tests were recorded.
10. WebView2 terminal focus/accessibility/theme behavior was tested.
11. Every FAIL/PARTIAL includes a concrete fix and affected files.
12. No unresolved mandatory item is hidden behind a numeric “compliance score.”

The app may be described as fully compliant with this audit contract only when:

```text
Mandatory FAIL       = 0
Mandatory PARTIAL    = 0
Mandatory NOT TESTED = 0
Release BLOCKER      = 0
```

A SHOULD-level deviation may remain only when the report documents a valid product/context reason and confirms that accessibility, native behavior, and usability are not degraded.

---

# 23. Official Microsoft sources

> Re-check the “Last updated” date on each page when running a later audit.

## Core design hub

- Design guidelines: https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview
- Windows apps design: https://learn.microsoft.com/en-us/windows/apps/design/

## Visual foundations

- Color: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/color
- Geometry: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/geometry
- Typography: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography
- Materials: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/materials
- Motion: https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/motion
- Iconography: https://learn.microsoft.com/en-us/windows/apps/design/iconography/
- Segoe Fluent Icons: https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-fluent-icons-font

## Layout and navigation

- Layout: https://learn.microsoft.com/en-us/windows/apps/design/layout/
- Responsive design: https://learn.microsoft.com/en-us/windows/apps/design/layout/responsive-design
- Screen sizes and breakpoints: https://learn.microsoft.com/en-us/windows/apps/design/layout/screen-sizes-and-breakpoints-for-responsive-design
- Navigation basics: https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics

## Controls and commanding

- Controls for Windows apps: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/
- Commanding basics: https://learn.microsoft.com/en-us/windows/apps/design/basics/commanding-basics
- Commanding implementation: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/commanding
- Command bar: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/command-bar

## Window and title bar

- Title bar design: https://learn.microsoft.com/en-us/windows/apps/design/basics/titlebar-design
- Title bar customization: https://learn.microsoft.com/en-us/windows/apps/develop/title-bar
- Windows app best practices: https://learn.microsoft.com/en-us/windows/apps/get-started/best-practices

## Theme and materials implementation

- Theming: https://learn.microsoft.com/en-us/windows/apps/develop/ui/theming
- Materials implementation: https://learn.microsoft.com/en-us/windows/apps/develop/ui/materials

## Input and accessibility

- Develop accessible Windows apps: https://learn.microsoft.com/en-us/windows/apps/develop/accessibility
- Accessibility checklist: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-checklist
- Keyboard interactions: https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-interactions
- Keyboard accelerators: https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-accelerators
- Touch target guidance: https://learn.microsoft.com/en-us/windows/apps/develop/input/guidelines-for-targeting

## Content and settings

- Writing style: https://learn.microsoft.com/en-us/windows/apps/design/style/writing-style
- App settings: https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings

## Reference implementation

- WinUI 3 Gallery and samples: https://learn.microsoft.com/en-us/windows/apps/get-started/samples

---

# 24. Final instruction to the coding agent

Do not return only general comments such as:

```text
The UI looks modern.
The app follows Fluent Design.
The colors look like Windows 11.
```

Return evidence. A valid finding must connect:

```text
Microsoft rule
→ current DevDeck implementation
→ concrete user impact
→ exact code/design correction
→ verification method
```

The highest-priority review order is:

```text
1. Data safety and destructive behavior
2. Keyboard traps and core accessibility
3. Custom title bar and window/Snap behavior
4. Theme, High Contrast, and hard-coded colors
5. Native control semantics and focus
6. Responsive/Snap/text-scaling behavior
7. WebView2 terminal accessibility and focus
8. Geometry, typography, materials, icons, and visual polish
```
