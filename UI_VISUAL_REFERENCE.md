# UI Visual Reference 🎨

## Component Showcase

This document provides a visual reference for all UI components and their states.

---

## 🎨 Color Palette

### Light Mode
```
Primary Colors:
┌─────────────────────────────────────┐
│ Primary:   #2563eb  ████████████    │
│ Success:   #10b981  ████████████    │
│ Warning:   #f59e0b  ████████████    │
│ Danger:    #ef4444  ████████████    │
└─────────────────────────────────────┘

Background Colors:
┌─────────────────────────────────────┐
│ Primary:   #ffffff  ████████████    │
│ Secondary: #f8f9fa  ████████████    │
│ Tertiary:  #f3f4f6  ████████████    │
└─────────────────────────────────────┘

Text Colors:
┌─────────────────────────────────────┐
│ Primary:   #1f2937  ████████████    │
│ Secondary: #6b7280  ████████████    │
│ Tertiary:  #9ca3af  ████████████    │
└─────────────────────────────────────┘
```

### Dark Mode
```
Background Colors:
┌─────────────────────────────────────┐
│ Primary:   #1a1a1a  ████████████    │
│ Secondary: #2d2d2d  ████████████    │
│ Tertiary:  #3a3a3a  ████████████    │
└─────────────────────────────────────┘

Text Colors:
┌─────────────────────────────────────┐
│ Primary:   #f3f4f6  ████████████    │
│ Secondary: #9ca3af  ████████████    │
│ Tertiary:  #6b7280  ████████████    │
└─────────────────────────────────────┘
```

---

## 📐 Layout Structure

```
┌─────────────────────────────────────────────────────────┐
│  HEADER                                                 │
│  ┌──────┐  AI Assistant                    ┌──┐  ┌──┐ │
│  │ 🤖  │  ● Ready                          │ + │  │ ☾ │ │
│  └──────┘                                   └──┘  └──┘ │
├─────────────────────────────────────────────────────────┤
│  CHAT CONTAINER                                         │
│                                                          │
│  ┌────────────────────────────────────────┐            │
│  │ User Message                           │            │
│  │ Right-aligned, blue gradient           │  👤        │
│  └────────────────────────────────────────┘            │
│                                                          │
│         ┌────────────────────────────────────────┐     │
│   🤖   │ Assistant Message                      │     │
│         │ Left-aligned, light background        │     │
│         │                                        │     │
│         │ ```csharp                              │     │
│         │ // Code block with syntax highlighting│     │
│         │ public class Example { }               │     │
│         │ ```                                    │     │
│         │ [Copy] [Execute]                       │     │
│         └────────────────────────────────────────┘     │
│                                                          │
├─────────────────────────────────────────────────────────┤
│  TYPING INDICATOR (when active)                         │
│  ● ● ●  AI is thinking...                              │
├─────────────────────────────────────────────────────────┤
│  INPUT AREA                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ [Customer] [Code] [Email]  Quick Actions        │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Ask me anything...                      📎  ➤  │   │
│  └─────────────────────────────────────────────────┘   │
│  0 / 5000    Ctrl+Enter to send, Shift+Enter for line  │
└─────────────────────────────────────────────────────────┘
```

---

## 💬 Message Components

### User Message
```
                    ┌────────────────────────────────┐
                    │ How do I find a customer?      │  👤
                    │                                │
                    └────────────────────────────────┘
                                            2:30 PM
```
**Styling:**
- Background: Linear gradient (#2563eb → #1e40af)
- Color: White
- Border radius: 18px 18px 4px 18px
- Max width: 75%
- Align: Right

### Assistant Message
```
┌────────────────────────────────────────┐
│ You can find a customer using:         │
│                                         │
│ ```csharp                               │
│ var customer = await FindCustomerAsync( │
│     "email@example.com"                 │
│ );                                      │
│ ```                                     │
│                                         │
│ [📋 Copy] [🔄 Regenerate]              │
└────────────────────────────────────────┘
2:30 PM
```
**Styling:**
- Background: #f3f4f6 (light) / #3a3a3a (dark)
- Border: 1px solid #e5e7eb
- Border radius: 18px 18px 18px 4px
- Max width: 75%
- Align: Left

---

## 🎯 Interactive Elements

### Code Block
```
┌─────────────────────────────────────────────────────┐
│ csharp                          [▶ Execute] [📋 Copy]│
├─────────────────────────────────────────────────────┤
│ public class Customer                                │
│ {                                                    │
│     public int Id { get; set; }                      │
│     public string Name { get; set; }                 │
│ }                                                    │
└─────────────────────────────────────────────────────┘
```

**States:**
- **Default**: Gray buttons
- **Hover**: Lighter background
- **Copied**: Green checkmark "✓ Copied!"
- **Executing**: "⏳ Running..."

### Quick Action Chips
```
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ 👥 Customer Cmds │  │ 💻 Generate Code │  │ ✉️ Email Help   │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

**States:**
- **Default**: Light gray background
- **Hover**: Blue tint, slight lift
- **Active**: Blue background

### Buttons

#### Primary Button (Send)
```
┌──────────┐
│ ➤ Send  │  ← Default (blue)
└──────────┘

┌──────────┐
│ ➤ Send  │  ← Hover (darker blue)
└──────────┘

┌──────────┐
│ ➤ Send  │  ← Disabled (gray)
└──────────┘
```

#### Icon Buttons
```
┌────┐  ┌────┐  ┌────┐
│ +  │  │ ☾  │  │ ⚙  │
└────┘  └────┘  └────┘
```

---

## 🎭 States & Animations

### Typing Indicator
```
Frame 1:  ● ○ ○  AI is thinking...
Frame 2:  ○ ● ○  AI is thinking...
Frame 3:  ○ ○ ●  AI is thinking...
Frame 4:  ○ ● ○  AI is thinking...
```
**Animation**: 1.4s infinite, staggered

### Message Entrance
```
Step 1: Opacity 0, translateY(10px)
Step 2: Opacity 0.5, translateY(5px)
Step 3: Opacity 1, translateY(0)
```
**Duration**: 300ms ease

### Theme Toggle
```
Light Mode:  ☾ (moon icon)
             ↓ (rotate 180°)
Dark Mode:   ☀ (sun icon)
```
**Duration**: 200ms ease

---

## 🎨 Component States

### Input Field
```
Default:
┌─────────────────────────────────────────┐
│ Ask me anything...                      │
└─────────────────────────────────────────┘

Focused:
┌─────────────────────────────────────────┐
│ How do I|                                │  ← Blue border + shadow
└─────────────────────────────────────────┘

With Text:
┌─────────────────────────────────────────┐
│ How do I find a customer?               │
└─────────────────────────────────────────┘
0 / 5000

Near Limit (4000+):
┌─────────────────────────────────────────┐
│ Very long text...                       │
└─────────────────────────────────────────┘
4523 / 5000  ← Orange

At Limit (4500+):
┌─────────────────────────────────────────┐
│ Very long text...                       │
└─────────────────────────────────────────┘
4876 / 5000  ← Red
```

### Error Message
```
┌─────────────────────────────────────────────────────┐
│ ⚠️  Connection failed. Please try again.            │
│                                        [🔄 Retry]   │
└─────────────────────────────────────────────────────┘
```
**Styling:**
- Background: #fef2f2 (light) / #7f1d1d (dark)
- Border: 1px solid #fecaca
- Color: #991b1b

### Toast Notification
```
┌─────────────────────────────────────────┐
│ ✓  Code copied to clipboard             │  ← Success
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ ⚠  An error occurred                    │  ← Error
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ ⓘ  Processing your request...           │  ← Info
└─────────────────────────────────────────┘
```
**Animation**: Slide in from right, auto-dismiss after 3s

---

## 📱 Responsive Breakpoints

### Desktop (> 768px)
```
┌─────────────────────────────────────────────────────────┐
│  Full width layout                                      │
│  Messages: 75% max width                                │
│  All features visible                                   │
│  Keyboard hints shown                                   │
└─────────────────────────────────────────────────────────┘
```

### Tablet (768px)
```
┌───────────────────────────────────────────┐
│  Adjusted layout                          │
│  Messages: 80% max width                  │
│  Touch-optimized buttons                  │
└───────────────────────────────────────────┘
```

### Mobile (< 768px)
```
┌─────────────────────────────┐
│  Compact layout             │
│  Messages: 85% max width    │
│  Keyboard hints hidden      │
│  Larger touch targets       │
└─────────────────────────────┘
```

---

## 🎨 Typography

### Font Families
```
Body:  'Segoe UI', system-ui, sans-serif
Code:  'Cascadia Code', 'Consolas', monospace
```

### Font Sizes
```
Header:        18px (bold)
Body:          14px
Small:         12px
Tiny:          11px
Code:          13px
```

### Line Heights
```
Body:          1.5
Code:          1.5
Headings:      1.2
```

---

## 🎯 Spacing System

```
--space-1:  0.25rem  (4px)
--space-2:  0.5rem   (8px)
--space-3:  0.75rem  (12px)
--space-4:  1rem     (16px)
--space-6:  1.5rem   (24px)
--space-8:  2rem     (32px)
```

**Usage:**
- Padding: space-3 to space-4
- Margins: space-4 to space-6
- Gaps: space-2 to space-4

---

## 🎭 Shadow System

```
--shadow-sm:  0 1px 2px 0 rgba(0, 0, 0, 0.05)
              ↓ Subtle elevation

--shadow-md:  0 4px 6px -1px rgba(0, 0, 0, 0.1)
              ↓ Medium elevation

--shadow-lg:  0 10px 15px -3px rgba(0, 0, 0, 0.1)
              ↓ High elevation
```

**Usage:**
- Cards: shadow-sm
- Buttons: shadow-md
- Modals: shadow-lg

---

## 🎨 Border Radius

```
--radius-sm:  6px   ← Buttons, chips
--radius-md:  8px   ← Input fields, code blocks
--radius-lg:  12px  ← Message bubbles
--radius-xl:  16px  ← Cards, panels
```

---

## 🎯 Icon Reference

### Bootstrap Icons Used
```
bi-robot          🤖  AI avatar
bi-person-fill    👤  User avatar
bi-plus-circle    ➕  New chat
bi-moon-fill      🌙  Dark mode
bi-sun-fill       ☀️  Light mode
bi-send-fill      ➤   Send message
bi-clipboard      📋  Copy
bi-check          ✓   Success
bi-play-fill      ▶️  Execute
bi-arrow-clockwise 🔄  Retry/Regenerate
bi-paperclip      📎  Attach
bi-people         👥  Customers
bi-code-square    💻  Code
bi-envelope       ✉️  Email
bi-exclamation-triangle ⚠️  Warning
bi-check-circle   ✓   Success toast
bi-x              ✕   Close
```

---

## 🎨 Animation Timings

```
Fast:     150ms  ← Hover effects
Normal:   250ms  ← Transitions
Slow:     300ms  ← Entrances
Theme:    300ms  ← Theme switching
```

**Easing:**
- Default: `ease`
- Smooth: `cubic-bezier(0.4, 0, 0.2, 1)`

---

## 📊 Visual Hierarchy

### Z-Index Layers
```
Layer 5:  1000  ← Toasts
Layer 4:  100   ← Modals
Layer 3:  10    ← Header
Layer 2:  5     ← Sticky elements
Layer 1:  1     ← Elevated content
Layer 0:  0     ← Base content
```

---

## 🎨 Accessibility

### Focus Indicators
```
Default:
┌──────────┐
│ Button   │
└──────────┘

Focused:
┌──────────┐
│ Button   │  ← Blue outline
└──────────┘
```

### Color Contrast
- Text on light: 4.5:1 minimum
- Text on dark: 4.5:1 minimum
- Interactive elements: 3:1 minimum

### Keyboard Navigation
- Tab order: Logical flow
- Focus visible: Always
- Skip links: Available

---

## 🎯 Component Checklist

### Header
- [x] AI avatar with icon
- [x] Title and status
- [x] New chat button
- [x] Theme toggle button
- [x] Gradient background

### Chat Container
- [x] Scrollable area
- [x] Auto-scroll to bottom
- [x] Message list
- [x] Smooth scrolling

### Messages
- [x] User messages (right)
- [x] Assistant messages (left)
- [x] Avatars
- [x] Timestamps
- [x] Action buttons

### Code Blocks
- [x] Language tag
- [x] Syntax highlighting
- [x] Copy button
- [x] Execute button
- [x] Dark background

### Input Area
- [x] Quick action chips
- [x] Auto-resize textarea
- [x] Character counter
- [x] Send button
- [x] Keyboard hints

### Feedback
- [x] Typing indicator
- [x] Loading states
- [x] Error messages
- [x] Toast notifications
- [x] Button feedback

---

## 🎨 Design Principles

1. **Clarity**: Clear visual hierarchy
2. **Consistency**: Uniform styling
3. **Feedback**: Visual responses
4. **Efficiency**: Quick actions
5. **Accessibility**: Inclusive design
6. **Delight**: Smooth animations

---

## 📐 Grid System

```
Container:  max-width: 900px
Gutter:     20px
Columns:    Flexible (CSS Grid)
```

---

## 🎯 Quick Reference

### Most Used Colors
```css
Primary:    var(--primary)      #2563eb
Success:    var(--success)      #10b981
Background: var(--bg-primary)   #ffffff
Text:       var(--text-primary) #1f2937
```

### Most Used Spacing
```css
Padding:    var(--space-4)      16px
Margin:     var(--space-4)      16px
Gap:        var(--space-3)      12px
```

### Most Used Radius
```css
Buttons:    var(--radius-md)    8px
Messages:   var(--radius-lg)    12px
```

---

**This visual reference provides a complete overview of all UI components, states, and styling used in the AI Assistant chat interface.**

*Last Updated: 2025-10-02*
