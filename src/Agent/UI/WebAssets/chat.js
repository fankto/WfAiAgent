// AI Assistant Chat Interface
// Handles UI interactions, markdown rendering, and C# communication

class ChatInterface {
    constructor() {
        this.messagesContainer = document.getElementById('messages');
        this.userInput = document.getElementById('user-input');
        this.sendBtn = document.getElementById('send-btn');
        this.typingIndicator = document.getElementById('typing-indicator');
        this.statusText = document.getElementById('status-text');
        this.charCount = document.getElementById('char-count');
        
        this.currentAssistantMessage = null;
        this.isProcessing = false;
        this.lastUserMessage = null;
        this.theme = localStorage.getItem('theme') || 'light';
        
        this.initializeMarked();
        this.setupEventListeners();
        this.setupCSharpCommunication();
        this.setupToastContainer();
        this.applyTheme();
        
        console.log('Chat interface initialized');
    }

    initializeMarked() {
        // Configure marked for markdown rendering
        marked.setOptions({
            highlight: function(code, lang) {
                if (lang && Prism.languages[lang]) {
                    return Prism.highlight(code, Prism.languages[lang], lang);
                }
                return code;
            },
            breaks: true,
            gfm: true
        });
    }

    setupEventListeners() {
        // Send button
        this.sendBtn.addEventListener('click', () => this.sendMessage());

        // Enter key handling
        this.userInput.addEventListener('keydown', (e) => {
            // Send: Enter (without Shift)
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
            
            // New chat: Ctrl/Cmd + N
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
                e.preventDefault();
                this.clearConversation();
            }
            
            // Clear input: Escape
            if (e.key === 'Escape') {
                this.userInput.value = '';
                this.updateCharCount();
                this.autoResizeTextarea();
            }
        });

        // Auto-resize textarea
        this.userInput.addEventListener('input', () => {
            this.autoResizeTextarea();
            this.updateCharCount();
        });

        // Quick action chips
        document.querySelectorAll('.quick-action-chip').forEach(chip => {
            chip.addEventListener('click', () => {
                const query = chip.getAttribute('data-query');
                this.userInput.value = query;
                this.userInput.focus();
            });
        });

        // New chat button
        document.getElementById('new-chat-btn')?.addEventListener('click', () => {
            this.clearConversation();
        });

        // Settings button
        document.getElementById('settings-btn')?.addEventListener('click', () => {
            this.toggleTheme();
        });
    }

    setupCSharpCommunication() {
        // Listen for messages from C# backend
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', (event) => {
                this.handleCSharpMessage(event.data);
            });
        }
    }

    handleCSharpMessage(data) {
        const message = typeof data === 'string' ? JSON.parse(data) : data;

        switch (message.type) {
            case 'token':
                this.appendToken(message.content);
                break;

            case 'complete':
                this.completeResponse();
                break;

            case 'error':
                this.showError(message.content);
                break;

            case 'status':
                this.updateStatus(message.content);
                break;

            default:
                console.warn('Unknown message type:', message.type);
        }
    }

    sendMessage() {
        const message = this.userInput.value.trim();
        if (!message || this.isProcessing) return;

        // Store for retry functionality
        this.lastUserMessage = message;

        // Add user message to UI
        this.addUserMessage(message);

        // Clear input
        this.userInput.value = '';
        this.updateCharCount();
        this.autoResizeTextarea();

        // Show typing indicator
        this.showTypingIndicator();
        this.isProcessing = true;
        this.sendBtn.disabled = true;

        // Send to C# backend
        this.sendToCSharp({
            type: 'user_query',
            content: message
        });

        // Prepare for assistant response
        this.currentAssistantMessage = this.createAssistantMessage();
    }

    addUserMessage(content) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message user';
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="message-bubble">${this.escapeHtml(content)}</div>
                <div class="message-time">${this.formatTime(new Date())}</div>
            </div>
            <div class="message-avatar">
                <i class="bi bi-person-fill"></i>
            </div>
        `;
        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }

    createAssistantMessage() {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message assistant';
        messageDiv.innerHTML = `
            <div class="message-avatar">
                <i class="bi bi-robot"></i>
            </div>
            <div class="message-content">
                <div class="message-bubble">
                    <div class="message-text"></div>
                </div>
                <div class="message-actions">
                    <button class="message-action-btn copy-btn">
                        <i class="bi bi-clipboard"></i> Copy
                    </button>
                    <button class="message-action-btn regenerate-btn">
                        <i class="bi bi-arrow-clockwise"></i> Regenerate
                    </button>
                </div>
                <div class="message-time">${this.formatTime(new Date())}</div>
            </div>
        `;

        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();

        // Setup action buttons
        const copyBtn = messageDiv.querySelector('.copy-btn');
        const regenerateBtn = messageDiv.querySelector('.regenerate-btn');

        copyBtn?.addEventListener('click', () => this.copyMessage(messageDiv));
        regenerateBtn?.addEventListener('click', () => this.regenerateMessage());

        return messageDiv;
    }

    appendToken(token) {
        if (!this.currentAssistantMessage) return;

        const textDiv = this.currentAssistantMessage.querySelector('.message-text');
        if (textDiv) {
            textDiv.textContent += token;
            this.scrollToBottom();
        }
    }

    completeResponse() {
        this.hideTypingIndicator();
        this.isProcessing = false;
        this.sendBtn.disabled = false;
        this.userInput.focus();

        // Render markdown
        if (this.currentAssistantMessage) {
            const textDiv = this.currentAssistantMessage.querySelector('.message-text');
            if (textDiv) {
                const rawText = textDiv.textContent;
                textDiv.innerHTML = this.renderMarkdown(rawText);
                this.addCodeCopyButtons(textDiv);
            }
        }

        this.currentAssistantMessage = null;
        this.updateStatus('Ready');
    }

    renderMarkdown(text) {
        try {
            return marked.parse(text);
        } catch (e) {
            console.error('Markdown rendering error:', e);
            return this.escapeHtml(text);
        }
    }

    addCodeCopyButtons(container) {
        const codeBlocks = container.querySelectorAll('pre code');
        codeBlocks.forEach((codeBlock, index) => {
            const pre = codeBlock.parentElement;
            
            // Detect language
            const className = codeBlock.className;
            const langMatch = className.match(/language-(\w+)/);
            const language = langMatch ? langMatch[1] : 'text';

            // Check if it's workflow code
            const isWorkflowCode = language === 'workflow' || language === 'csharp';

            // Create header
            const header = document.createElement('div');
            header.className = 'code-header';
            header.innerHTML = `
                <span class="code-language">${language}</span>
                <div style="display: flex; gap: 8px;">
                    ${isWorkflowCode ? `
                        <button class="code-execute-btn" data-code-index="${index}">
                            <i class="bi bi-play-fill"></i> Execute
                        </button>
                    ` : ''}
                    <button class="code-copy-btn" data-code-index="${index}">
                        <i class="bi bi-clipboard"></i> Copy
                    </button>
                </div>
            `;

            pre.insertBefore(header, codeBlock);

            // Setup copy button
            const copyBtn = header.querySelector('.code-copy-btn');
            copyBtn?.addEventListener('click', () => {
                this.copyCode(codeBlock.textContent, copyBtn);
            });

            // Setup execute button
            if (isWorkflowCode) {
                const executeBtn = header.querySelector('.code-execute-btn');
                executeBtn?.addEventListener('click', () => {
                    this.executeCode(codeBlock.textContent, executeBtn);
                });
            }
        });
    }

    copyCode(code, button) {
        this.sendToCSharp({
            type: 'copy_code',
            content: code
        });

        // Visual feedback
        const originalHtml = button.innerHTML;
        button.innerHTML = '<i class="bi bi-check"></i> Copied!';
        button.classList.add('success');

        setTimeout(() => {
            button.innerHTML = originalHtml;
            button.classList.remove('success');
        }, 2000);

        // Show toast notification
        this.showToast('Code copied to clipboard', 'success');
    }

    executeCode(code, button) {
        // Send to C# backend to execute
        this.sendToCSharp({
            type: 'execute_code',
            content: code
        });

        // Visual feedback
        const originalHtml = button.innerHTML;
        button.innerHTML = '<i class="bi bi-hourglass-split"></i> Running...';
        button.disabled = true;

        // Re-enable after 3 seconds (C# will send actual completion)
        setTimeout(() => {
            button.innerHTML = originalHtml;
            button.disabled = false;
        }, 3000);

        this.showToast('Executing code...', 'success');
    }

    copyMessage(messageDiv) {
        const textDiv = messageDiv.querySelector('.message-text');
        if (textDiv) {
            const text = textDiv.textContent;
            this.sendToCSharp({
                type: 'copy_code',
                content: text
            });

            // Visual feedback
            const copyBtn = messageDiv.querySelector('.copy-btn');
            if (copyBtn) {
                const originalHtml = copyBtn.innerHTML;
                copyBtn.innerHTML = '<i class="bi bi-check"></i> Copied!';
                copyBtn.classList.add('success');

                setTimeout(() => {
                    copyBtn.innerHTML = originalHtml;
                    copyBtn.classList.remove('success');
                }, 2000);
            }
        }
    }

    regenerateMessage() {
        if (this.lastUserMessage && !this.isProcessing) {
            // Remove last assistant message
            const messages = this.messagesContainer.querySelectorAll('.message.assistant');
            if (messages.length > 0) {
                messages[messages.length - 1].remove();
            }
            
            // Resend the last user message
            this.retryLastMessage();
        }
    }

    showError(errorMessage) {
        this.hideTypingIndicator();
        this.isProcessing = false;
        this.sendBtn.disabled = false;

        if (this.currentAssistantMessage) {
            const textDiv = this.currentAssistantMessage.querySelector('.message-text');
            if (textDiv) {
                textDiv.innerHTML = `
                    <div class="error-message">
                        <span class="error-icon">⚠️</span>
                        <div class="error-content">
                            <span class="error-text">${this.escapeHtml(errorMessage)}</span>
                            <button class="retry-button" onclick="window.chatInterface.retryLastMessage()">
                                <i class="bi bi-arrow-clockwise"></i> Retry
                            </button>
                        </div>
                    </div>
                `;
            }
        }

        this.currentAssistantMessage = null;
        this.updateStatus('Error', 'danger');
        this.showToast('An error occurred', 'error');
    }

    retryLastMessage() {
        if (this.lastUserMessage && !this.isProcessing) {
            this.userInput.value = this.lastUserMessage;
            this.sendMessage();
        }
    }

    showTypingIndicator() {
        this.typingIndicator.classList.remove('hidden');
        this.updateStatus('Thinking...');
    }

    hideTypingIndicator() {
        this.typingIndicator.classList.add('hidden');
    }

    updateStatus(text, type = 'success') {
        this.statusText.textContent = text;
        const statusDot = document.querySelector('.status-dot');
        
        if (statusDot) {
            statusDot.style.background = type === 'danger' ? '#ef4444' : '#10b981';
        }
    }

    clearConversation() {
        if (confirm('Clear conversation history?')) {
            this.messagesContainer.innerHTML = '';
            this.sendToCSharp({
                type: 'clear_conversation'
            });
            this.addWelcomeMessage();
        }
    }

    addWelcomeMessage() {
        const welcomeDiv = document.createElement('div');
        welcomeDiv.className = 'message assistant';
        welcomeDiv.innerHTML = `
            <div class="message-avatar">
                <i class="bi bi-robot"></i>
            </div>
            <div class="message-content">
                <div class="message-bubble">
                    <div class="message-text">
                        <p>👋 Hi! I'm your Workflow+ AI Assistant.</p>
                        <p>I can help you:</p>
                        <ul>
                            <li>Find commands and functions</li>
                            <li>Generate script code</li>
                            <li>Explain syntax and parameters</li>
                            <li>Check license requirements</li>
                        </ul>
                        <p>Try asking me something like "How do I find a customer by name?"</p>
                    </div>
                </div>
            </div>
        `;
        this.messagesContainer.appendChild(welcomeDiv);
    }

    setupToastContainer() {
        if (!document.querySelector('.toast-container')) {
            const container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }
    }

    showToast(message, type = 'success', duration = 3000) {
        const container = document.querySelector('.toast-container');
        if (!container) return;

        const icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-exclamation-circle-fill',
            warning: 'bi-exclamation-triangle-fill'
        };

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.innerHTML = `
            <i class="bi ${icons[type]} toast-icon"></i>
            <div class="toast-content">
                <div class="toast-message">${this.escapeHtml(message)}</div>
            </div>
            <button class="toast-close">
                <i class="bi bi-x"></i>
            </button>
        `;

        container.appendChild(toast);

        // Close button
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn?.addEventListener('click', () => {
            toast.remove();
        });

        // Auto remove
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }

    toggleTheme() {
        this.theme = this.theme === 'light' ? 'dark' : 'light';
        this.applyTheme();
        localStorage.setItem('theme', this.theme);
        this.showToast(`${this.theme === 'dark' ? 'Dark' : 'Light'} mode enabled`, 'success');
    }

    applyTheme() {
        document.documentElement.setAttribute('data-theme', this.theme);
        
        // Update settings button icon
        const settingsBtn = document.getElementById('settings-btn');
        if (settingsBtn) {
            const icon = settingsBtn.querySelector('i');
            if (icon) {
                icon.className = this.theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
            }
        }
    }

    autoResizeTextarea() {
        this.userInput.style.height = 'auto';
        this.userInput.style.height = Math.min(this.userInput.scrollHeight, 120) + 'px';
    }

    updateCharCount() {
        const count = this.userInput.value.length;
        this.charCount.textContent = `${count} / 5000`;
        
        if (count > 4500) {
            this.charCount.style.color = '#ef4444';
        } else if (count > 4000) {
            this.charCount.style.color = '#f59e0b';
        } else {
            this.charCount.style.color = 'var(--text-tertiary)';
        }
    }

    scrollToBottom() {
        const scrollAnchor = document.getElementById('scroll-anchor');
        if (scrollAnchor) {
            scrollAnchor.scrollIntoView({ behavior: 'smooth', block: 'end' });
        } else {
            this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
        }
    }

    sendToCSharp(message) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(message));
        } else {
            console.warn('WebView2 not available. Message:', message);
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    formatTime(date) {
        return date.toLocaleTimeString('en-US', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.chatInterface = new ChatInterface();
    window.chatInterface.addWelcomeMessage();
    
    // Focus input
    window.chatInterface.userInput.focus();
});
