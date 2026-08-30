# Zebrahoof EMR — User Guide

This guide describes what you see when using **Zebrahoof EMR** as a clinician or staff member: main layouts, navigation, screens, and common buttons. Some areas use demonstration data for training and prototyping.

---

## Signing in and signing out

### Login (`/login`)

- **Sign In** — Submits your username or email and password.
- **Remember me** — Keeps your session longer when supported by your browser.
- **Forgot password?** — Opens password recovery.
- **Patient registration** — Opens self-registration for patients (when enabled).
- **Password field** — Eye icon toggles showing or hiding the password.

After a successful sign-in, you may be sent to **multi-factor authentication** if your account requires it (`/mfa-challenge`). Setup for MFA is at `/mfa-setup`.

### Profile menu (top-right)

- Your **name** and **role** (for example Physician, Nurse, Administrator).
- **My Profile** — Opens your profile page.
- **Preferences** — Opens preferences.
- **Current Role** — Shows your active role chip.
- **Log out** — Ends your session and returns to login.

---

## Main layout (every page after login)

### Top app bar

| Control | What it does |
|--------|----------------|
| **Menu** (hamburger) | Opens or collapses the **left navigation drawer**. The drawer can stay as a slim strip and expand when you hover. |
| **Zebrahoof EMR** logo / title | Branding; home area is the dashboard. |
| **Search patients...** | Global search: type at least two characters, pick a patient from the list to open their chart. |
| **Location** chip | Opens a menu to switch your **current location** (clinic site). The active location shows a checkmark. |
| **Notifications** (bell) | Opens the **Inbox**; a badge can show unread count. |
| **Dark mode / Light mode** (sun or moon) | Switches the app between dark and light theme. Tooltip: “Switch to Dark Mode” or “Switch to Light Mode”. |
| **Profile** area | Opens the profile menu described above. |

### Left navigation (drawer)

Sections and links:

**Clinical**

| Link | Opens |
|------|--------|
| **Dashboard** | Home overview (`/` or `/dashboard`). |
| **Patients** | Patient list and charts (`/patients`). |
| **Schedule** | Appointments calendar (`/schedule`). |
| **Tasks** | Task list (`/tasks`). |
| **Orders** | Orders placeholder (`/orders`) — see note below. |

**Communication**

| Link | Opens |
|------|--------|
| **Inbox** | Staff inbox; may show an **unread** count badge. |
| **Patient Messages** | Form to simulate a **patient-sent** message into the inbox (`/patient-messages`). |

**Administration**

| Link | Opens |
|------|--------|
| **Settings** | System settings (`/admin/settings`). |
| **Local AI** | Download and run the on-machine Qwen engine (`/admin/local-ai`). |
| **Templates** | Note and documentation templates (`/admin/templates`). |
| **Sticky Notes** | Adds or manages personal sticky notes (menu shows count, e.g. `3/5`). Click the chip menu to show or hide individual notes. |
| **Users** | User management (`/admin/users`) — **visible to Administrator role** only. |

### Below the main content

- **Breadcrumbs** — When there is a trail, links appear under the app bar so you can step back through sections (for example Patients → a patient name).
- **Session idle** — A small **warning chip** may appear in the bottom-right (for example “Session idle: mm:ss”) when your session is nearing idle timeout. Staying active refreshes your session; if you are idle too long, you may be signed out for security.

### Blazor reconnect

If the connection to the server drops briefly, a **Reconnect** experience may appear so the page can restore without losing your place.

### Errors

If an error occurs, a bar may offer **Reload** to refresh the page.

---

## Dashboard (`/` or `/dashboard`)

Welcome line uses your display name.

**Cards** (typical):

| Card | Contents / actions |
|------|-------------------|
| **Today's Appointments** | Lists today’s visits; rows can open the patient chart. **Open-in-new** icon goes to **Schedule**. **View All** if there are more than five. |
| **Pending Tasks** | Tasks needing attention; **View All** goes to **Tasks**. |
| **Messages** | Unread-style summary; opens **Inbox** from rows or **View All**. |
| **Recent Interactions** | Recent activity; items can open the patient chart. Link to **Patients**. |
| **Clinical Alerts** | Pending alerts when present. |

---

## Patients — list view (`/patients`)

| Control | What it does |
|--------|----------------|
| **New Patient** | Opens the **New Patient** dialog: fields include First Name, Last Name, Date of Birth, Sex, and additional sections for contact and clinical info. Complete the form and save to add a patient. |
| **Search by name, MRN, DOB, phone...** | Filters the list as you type (debounced). |
| **Filters** | Toggles an advanced filter panel. When filters are on, the button shows **Filters (n)** with the count of active filters. |
| **Provider**, **Status**, **Last Seen**, **Sex** (in filter panel) | Narrow the list. |
| **Clear All** / **Apply Filters** | Reset or apply advanced filters. |

**Table**

- Column headers **Name**, **DOB**, **Last Visit** support sorting.
- **Name** — Link into the patient chart.
- **Alerts** — Chips may show allergy and alert counts; hover for details.
- Row actions: **View Chart** (eye), **Edit Demographics** (pencil), **Schedule Appointment** (calendar) — schedule opens the Schedule page.

**Pager** — Choose rows per page (5, 10, 25, 50).

---

## Patients — chart view (`/patients/{id}/{tab}`)

Opens when you select a patient from the list or global search.

### Patient banner (header)

- **Back** — Returns to the patient list.
- Patient **avatar**, **full name**, **MRN**, **DOB**, **age**, **sex**, **primary provider**.
- **Allergy** chips (or **NKA** if none).
- **Alert** chips.
- **New Note** — Quick action (documentation workflow).
- **Appointments** — Opens an **Appointments** dialog for that patient; from there you can go to scheduling.
- **⋮ More** menu — **Print Summary**, **Send Message**, **Create Task**.

### Under the banner

- **Inbox-style messages** for this patient may appear here so you can read chart-related messages.
- **Send to local AI** / **Documents Received** — Sends uploaded chart documents to the on-machine Qwen engine when it is installed; shows progress while sending and then a completed state. Install the engine from **Admin → Local AI**.
- **Patient sticky note** (pink floating button) — Opens a **patient-specific** sticky note panel.

### Chart tabs

You can **drag tabs** to reorder them; order may be remembered in the browser.

| Tab | Shows |
|-----|--------|
| **Encounter** | Encounter-focused content for the patient. |
| **Summary** | High-level chart summary. |
| **Problems** | Problem list. |
| **Medications** | Medications. |
| **Allergies** | Allergies. |
| **History** | Medical history. |
| **Labs** | Lab results. |
| **Imaging** | Imaging. |
| **Vitals** | Vital signs. |
| **Immunizations** | Immunizations. |
| **Documents** | Stored documents (open/download may use separate document actions in the UI). |
| **Notes** | Clinical notes. |
| **Care Team** | Care team members. |
| **Demographics** | Demographics and registration details. |

### Notes tree (optional route)

- **`/patients/{id}/notes/tree`** — Hierarchical view of notes for that patient.

---

## Schedule (`/schedule`)

| Control | What it does |
|--------|----------------|
| **New Appointment** | Opens a dialog to create an appointment. |
| **Previous / Next** (chevrons) | Move the visible day or week. |
| **Today** | Jumps to today’s date. |
| **Date picker** | Choose a specific date. |
| **Provider** | **All Providers** or a specific provider. |
| **Day** / **Week** | Switches calendar density. |

Clicking an **hour row** may start booking at that time. Appointment blocks on the grid can be selected for details (depending on implementation).

---

## Tasks (`/tasks`)

| Area | What it does |
|------|----------------|
| **New Task** | Opens the new-task dialog. |
| Summary tiles | **Pending**, **Overdue**, **Due Today**, **Completed Today** — some tiles filter the list when clicked. |
| **All** / **Pending** / **Overdue** / **Completed** | Filter chips for the task list. |
| **Type** | Drop-down: **All Types** or a specific task type. |
| **Assignee** | **All Assignees** or a specific person. |
| List header | **View** toggle (list vs compact icons) changes how tasks display. |
| Task rows | Clicking a row often opens a **detail** drawer or panel. |

---

## Orders (`/orders`)

This page is a **placeholder** for future **computerized provider order entry (CPOE)**. It lists planned capabilities (medications, labs, imaging, referrals, order cart). No live ordering workflow is available here yet.

---

## Inbox (`/inbox`)

| Control | What it does |
|--------|----------------|
| **Compose** | Opens **New Message**: **To**, **Subject**, **Message**, then **Send** or **Cancel**. |
| Tabs | **All**, **Messages**, **Results**, **Refills**, **Admin** — each filters the left message list. |
| **Unread** / **Flagged** chips | Toggle quick filters. |
| Message list | Select a message to read it on the right. |
| **Flag** | Toggle flagged state on the open message. |
| **Archive** / **Delete** | Icons in the message header (availability depends on implementation). |
| **Reply** | Opens reply dialog with **Cancel** and **Send**. |
| **Forward** | Forward flow (outlined button). |

Patient-linked items may open in the **patient chart** message area under the banner.

---

## Patient Messages (`/patient-messages`)

Demo utility to simulate a **message from a patient** into the system:

- **Patient** — Choose who the message is from.
- **Subject**, **Message (from patient)**.
- **Send to inbox** — Delivers the message to **Inbox** and the patient chart flow.

---

## Encounter workspace (`/encounter/{EncounterId}`)

- **Back** — Returns using browser history (previous page).

### Encounter banner

- Visit **type**, **date/time**, **provider**, **location**, **status** chip (for example In Progress, Signed).
- For **In Progress**: **Sign Note**, **Save Draft**.
- For signed visits: **Add Addendum**.

### Workspace panels

Typically includes **Patient**, **Active Problems**, **Current Medications**, **Allergies**, and a main documentation area (note editor, review of systems, physical exam, etc., depending on encounter state).

---

## Profile and preferences

- **`/profile`** — **My Profile** from the menu.
- **`/preferences`** — **Preferences** (display and workflow options as implemented).

---

## Administration (role-dependent)

| Page | Purpose |
|------|---------|
| **`/admin`** | Admin home with cards linking to users, locations, templates, settings, sessions, audit. |
| **`/admin/users`** | Users and roles. |
| **`/admin/locations`** | Locations and departments. |
| **`/admin/templates`** | Template management. |
| **`/admin/settings`** | Application settings. |
| **`/admin/local-ai`** | Download Ollama, pick Qwen / DeepSeek / Kimi / other open models, stop a download, and see warnings if a model will not fit this PC. Chart AI stays on this machine. |
| **`/admin/sessions`** | Active session management. |
| **`/admin/audit-log`** | Audit log review. |

Exact buttons on each admin screen follow the labels shown in the app (Save, Add, Edit, and so on).

---

## Access and security messages

- **`/access-denied`** — You tried to open a page your role does not allow.
- After **logout**, the login page may show a success indicator.

---

## Personal sticky notes (nav)

From **Sticky Notes** in the left nav:

- The menu shows how many notes you have out of the maximum (for example **3/5**).
- You can add a new note if you are under the cap.
- Per-note menu items can **show or hide** individual notes.

These are personal workspace notes, separate from **patient** sticky notes on the chart.

---

## Quick reference — routes

| You want to… | Go to |
|--------------|--------|
| See your day at a glance | **Dashboard** |
| Find a patient | **Search patients** or **Patients** |
| Open a chart | Select a patient → chart tabs |
| Book or view appointments | **Schedule** |
| Work your queue | **Tasks** |
| Read or send internal messages | **Inbox** |
| Simulate a patient portal message | **Patient Messages** |
| Full encounter editor | Open an encounter (from chart/workflow) → `/encounter/{id}` |
| Change site | **Location** in the app bar |
| Sign out | **Log out** in the profile menu |

---

*This guide matches the Zebrahoof EMR user interface as implemented in the product. Labels and availability may vary slightly by environment and role.*
