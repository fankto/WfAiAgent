# UI Testing Guide 🧪

## Quick Start Testing

### 1. Build the Project
```bash
cd AiAgent/src/Agent
dotnet build
```

### 2. Verify Embedded Resources
Check that the WebAssets are included:
```bash
dotnet build -v detailed | grep WebAssets
```

You should see:
```
UI\WebAssets\chat.html
UI\WebAssets\chat.css
UI\WebAssets\chat.js
```

### 3. Run the Application
```bash
dotnet run
```

---

## Manual Testing Checklist

### Basic Functionality ✅

#### Message Sending
- [ ] Type a message and press Enter
- [ ] Message appears in chat with blue bubble
- [ ] Input clears after sending
- [ ] Typing indicator appears
- [ ] Assistant response streams in
- [ ] Response renders with markdown

#### Keyboard Shortcuts
- [ ] `Enter` sends message
- [ ] `Shift + Enter` creates new line
- [ ] `Ctrl/Cmd + N` clears conversation
- [ ] `Escape` clears input field

#### Quick Actions
- [ ] Click "Customer Commands" chip
- [ ] Query appears in input
- [ ] Click "Generate Code" chip
- [ ] Click "Email Help" chip

---

### Rich Content Rendering ✅

#### Markdown
Test with this message:
```
# Heading 1
## Heading 2

**Bold text** and *italic text*

- List item 1
- List item 2

1. Numbered item
2. Another item

[Link text](https://example.com)
```

Expected:
- [ ] Headings render correctly
- [ ] Bold and italic work
- [ ] Lists are formatted
- [ ] Links are clickable

#### Code Blocks
Test with this message:
```
Here's some C# code:

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```
```

Expected:
- [ ] Code block has dark background
- [ ] Syntax highlighting works
- [ ] Language tag shows "csharp"
- [ ] Copy button appears
- [ ] Execute button appears (for C#)

#### Inline Code
Test: `This is inline code`

Expected:
- [ ] Inline code has light background
- [ ] Monospace font

---

### Interactive Features ✅

#### Copy Code
- [ ] Click copy button on code block
- [ ] Button shows "Copied!" feedback
- [ ] Toast notification appears
- [ ] Code is in clipboard (paste to verify)
- [ ] Button returns to "Copy" after 2 seconds

#### Execute Code
- [ ] Click execute button on C# code
- [ ] Button shows "Running..." state
- [ ] Button is disabled during execution
- [ ] Toast notification appears
- [ ] Button re-enables after completion

#### Message Actions
- [ ] Hover over assistant message
- [ ] Copy and Regenerate buttons appear
- [ ] Click Copy button
- [ ] Message text copied to clipboard
- [ ] Click Regenerate button
- [ ] Last message removed and resent

---

### Theme Support ✅

#### Light Mode (Default)
- [ ] White background
- [ ] Dark text
- [ ] Blue primary color
- [ ] Clear contrast

#### Dark Mode
- [ ] Click theme toggle button (moon icon)
- [ ] Background turns dark (#1a1a1a)
- [ ] Text turns light
- [ ] Icon changes to sun
- [ ] Toast confirms theme change
- [ ] All elements adapt to dark theme

#### Theme Persistence
- [ ] Toggle to dark mode
- [ ] Refresh page
- [ ] Dark mode persists
- [ ] Toggle to light mode
- [ ] Refresh page
- [ ] Light mode persists

---

### Error Handling ✅

#### Network Error Simulation
To test error handling:
1. Stop the SignalR server
2. Send a message
3. Expected:
   - [ ] Error message appears
   - [ ] Error has warning icon
   - [ ] Retry button is visible
   - [ ] Click retry button
   - [ ] Message resends

#### Invalid Input
- [ ] Try sending empty message (should be blocked)
- [ ] Type 5000+ characters (counter turns red)
- [ ] Try sending at 5000 limit (should work)

---

### Animations & Transitions ✅

#### Message Animations
- [ ] New messages slide in from bottom
- [ ] Smooth opacity transition
- [ ] No jarring movements

#### Hover Effects
- [ ] Hover over user message (slight shift left)
- [ ] Hover over assistant message (slight shift right)
- [ ] Hover over buttons (color change)
- [ ] Hover over quick action chips (lift effect)

#### Theme Transition
- [ ] Toggle theme
- [ ] Smooth color transitions
- [ ] No flickering
- [ ] Icon rotates 180°

#### Typing Indicator
- [ ] Three dots animate
- [ ] Smooth up/down motion
- [ ] Staggered timing
- [ ] Continuous loop

---

### Responsive Design ✅

#### Desktop (> 768px)
- [ ] Messages max 75% width
- [ ] All features visible
- [ ] Keyboard hints shown
- [ ] Comfortable spacing

#### Tablet (768px)
- [ ] Messages max 85% width
- [ ] Layout adjusts
- [ ] Touch targets adequate

#### Mobile (< 768px)
- [ ] Messages max 85% width
- [ ] Keyboard hints hidden
- [ ] Touch-optimized buttons
- [ ] Scrolling smooth

Test by resizing browser window.

---

### Accessibility ✅

#### Keyboard Navigation
- [ ] Tab through all interactive elements
- [ ] Focus indicators visible
- [ ] Enter activates buttons
- [ ] Escape clears input

#### Reduced Motion
Enable "Reduce motion" in OS settings:
- [ ] Animations are minimal
- [ ] No jarring transitions
- [ ] Functionality preserved

#### Screen Reader (Optional)
- [ ] Messages are announced
- [ ] Buttons have labels
- [ ] Status updates announced

---

### Performance ✅

#### Streaming
- [ ] Tokens appear smoothly
- [ ] No lag or stuttering
- [ ] Auto-scroll works
- [ ] Long responses handle well

#### Large Conversations
Send 20+ messages:
- [ ] Scrolling remains smooth
- [ ] No memory leaks
- [ ] Performance stable

#### Code Blocks
Send message with 10+ code blocks:
- [ ] All render correctly
- [ ] Syntax highlighting works
- [ ] Copy buttons all functional
- [ ] No performance issues

---

## Automated Testing (Future)

### Unit Tests
```csharp
[Fact]
public void ChatInterface_SendMessage_AddsToContainer()
{
    // Test message sending
}

[Fact]
public void ChatInterface_ToggleTheme_UpdatesDOM()
{
    // Test theme switching
}
```

### Integration Tests
```csharp
[Fact]
public async Task WebView_LoadsResources_Successfully()
{
    // Test resource loading
}

[Fact]
public async Task SignalR_StreamsTokens_ToWebView()
{
    // Test streaming
}
```

---

## Common Issues & Solutions

### Issue: HTML doesn't load
**Solution**: 
1. Check embedded resources are built
2. Verify file paths in csproj
3. Check console for errors
4. Try file system fallback

### Issue: Markdown not rendering
**Solution**:
1. Check Marked.js CDN loads
2. Verify internet connection
3. Check browser console
4. Test with simple markdown

### Issue: Syntax highlighting missing
**Solution**:
1. Check Prism.js CDN loads
2. Verify language is supported
3. Check code block format
4. Test with known language

### Issue: Theme not persisting
**Solution**:
1. Check localStorage access
2. Verify browser allows storage
3. Check console for errors
4. Clear browser cache

### Issue: Copy button not working
**Solution**:
1. Check clipboard permissions
2. Verify C# message handler
3. Test with simple text
4. Check browser console

---

## Performance Benchmarks

### Target Metrics
- **Initial Load**: < 500ms
- **Message Send**: < 100ms
- **Token Render**: < 16ms (60fps)
- **Theme Toggle**: < 300ms
- **Code Copy**: < 50ms

### Measuring Performance
```javascript
// In browser console
performance.mark('start');
// Perform action
performance.mark('end');
performance.measure('action', 'start', 'end');
console.log(performance.getEntriesByType('measure'));
```

---

## Browser Compatibility

### Supported Browsers (via WebView2)
- ✅ Edge (Chromium) - Primary
- ✅ Chrome - Via WebView2
- ⚠️ Firefox - Not supported (WebView2 limitation)
- ⚠️ Safari - Not supported (Windows only)

### Required Versions
- WebView2 Runtime: Latest
- .NET: 9.0+
- Windows: 10/11

---

## Debugging Tips

### Enable Verbose Logging
```csharp
_logger.Debug("WebView2 message: {Message}", json);
```

### Browser DevTools
Right-click in WebView2 → Inspect
- Check Console for errors
- Monitor Network tab
- Inspect Elements
- Debug JavaScript

### Common Console Commands
```javascript
// Check theme
console.log(document.documentElement.getAttribute('data-theme'));

// Check chat instance
console.log(window.chatInterface);

// Manually send message
window.chatInterface.sendMessage();

// Toggle theme
window.chatInterface.toggleTheme();
```

---

## Test Data

### Sample Queries
```
1. "How do I find a customer by email?"
2. "Generate code to update a customer's address"
3. "What commands are available for sending emails?"
4. "Show me an example of error handling"
5. "How do I query the database?"
```

### Sample Code Responses
```csharp
// Customer lookup
var customer = await FindCustomerAsync("john@example.com");
if (customer != null)
{
    Console.WriteLine($"Found: {customer.Name}");
}
```

```sql
-- Database query
SELECT * FROM Customers 
WHERE Email = 'john@example.com'
LIMIT 1;
```

---

## Success Criteria

### Must Have ✅
- [x] Messages send and receive
- [x] Markdown renders correctly
- [x] Code blocks have syntax highlighting
- [x] Copy functionality works
- [x] Theme toggle works
- [x] Keyboard shortcuts work
- [x] Error handling works
- [x] Responsive on all sizes

### Should Have ✅
- [x] Smooth animations
- [x] Toast notifications
- [x] Message actions
- [x] Quick action chips
- [x] Typing indicator
- [x] Auto-scroll
- [x] Character counter

### Nice to Have 🎯
- [ ] Conversation history
- [ ] Search functionality
- [ ] Export conversation
- [ ] Voice input
- [ ] File attachments

---

## Sign-Off Checklist

Before marking as complete:
- [ ] All basic functionality tests pass
- [ ] Rich content renders correctly
- [ ] Interactive features work
- [ ] Theme switching works
- [ ] Error handling works
- [ ] Animations are smooth
- [ ] Responsive on all sizes
- [ ] Accessibility features work
- [ ] Performance is acceptable
- [ ] No console errors
- [ ] Documentation is complete

---

## Next Steps

After testing:
1. ✅ Fix any issues found
2. ✅ Update documentation
3. ✅ Create demo video (optional)
4. ✅ Deploy to production
5. ✅ Gather user feedback
6. ✅ Plan next iteration

---

**Happy Testing! 🎉**

If you find any issues, document them with:
- Steps to reproduce
- Expected behavior
- Actual behavior
- Screenshots/videos
- Browser console logs
