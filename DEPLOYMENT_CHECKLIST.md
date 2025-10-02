# Deployment Checklist

Use this checklist to ensure the AI Agent is properly deployed and configured.

## Pre-Deployment

### Environment Setup
- [ ] .NET 9.0 SDK installed
- [ ] WebView2 Runtime installed (Windows 10+)
- [ ] OpenAI API key obtained
- [ ] AiSearch service accessible

### Configuration
- [ ] `appsettings.json` reviewed and customized
- [ ] `agent_config.yml` system prompts reviewed
- [ ] Cost limits configured appropriately
- [ ] Logging paths configured

### Security
- [ ] API key stored securely (DPAPI or environment variable)
- [ ] PII scrubbing enabled
- [ ] Audit logging configured
- [ ] Database file permissions set correctly

## Build & Test

### Build
- [ ] Solution builds without errors: `dotnet build`
- [ ] All projects compile successfully
- [ ] No compiler warnings (critical)
- [ ] Dependencies restored correctly

### Unit Tests
- [ ] All unit tests pass: `dotnet test`
- [ ] SearchKnowledgeTool tests pass
- [ ] ScriptValidationTool tests pass
- [ ] ConversationManager tests pass
- [ ] Test coverage > 70%

### Integration Tests
- [ ] AiSearch service running
- [ ] OpenAI API key configured
- [ ] End-to-end tests pass
- [ ] AiSearch integration tests pass

### Manual Testing
- [ ] Console app starts successfully
- [ ] Can process simple queries
- [ ] Code generation works
- [ ] Reflection/validation works
- [ ] Conversation persistence works
- [ ] Cost tracking works
- [ ] Error handling works (test with invalid API key)

## Deployment

### Package
- [ ] Build release version: `dotnet build -c Release`
- [ ] All dependencies included
- [ ] Configuration files included
- [ ] Documentation included

### Installation
- [ ] Copy files to deployment location
- [ ] Set file permissions
- [ ] Configure API key
- [ ] Test database creation
- [ ] Verify log file creation

### Integration
- [ ] WinForms plugin loads successfully
- [ ] UI panel displays correctly
- [ ] SignalR connection establishes
- [ ] WebView2 renders chat interface
- [ ] Streaming responses work

## Post-Deployment

### Verification
- [ ] Agent responds to queries
- [ ] Search integration works
- [ ] Code generation produces valid scripts
- [ ] Conversations save and load
- [ ] Costs are tracked correctly
- [ ] Logs are being written

### Monitoring
- [ ] Check logs for errors
- [ ] Monitor token usage
- [ ] Track query latency
- [ ] Review cost summaries
- [ ] Check database size

### Performance
- [ ] First token latency < 3s
- [ ] Full response time < 5s
- [ ] Search latency < 500ms
- [ ] Memory usage < 500MB
- [ ] No memory leaks

## User Acceptance

### Functionality
- [ ] Users can ask questions
- [ ] Responses are relevant
- [ ] Code is syntactically correct
- [ ] License warnings are clear
- [ ] Error messages are helpful

### Usability
- [ ] UI is intuitive
- [ ] Streaming feels responsive
- [ ] Conversation history works
- [ ] Search is fast
- [ ] Cost information is visible

### Documentation
- [ ] README is clear
- [ ] USAGE_EXAMPLES are helpful
- [ ] Configuration is documented
- [ ] Troubleshooting guide available

## Rollback Plan

### If Issues Occur
- [ ] Stop the agent service
- [ ] Backup database file
- [ ] Review logs for errors
- [ ] Restore previous version
- [ ] Document issues encountered

### Recovery Steps
1. Identify the issue from logs
2. Check configuration files
3. Verify external dependencies (AiSearch, OpenAI)
4. Test with minimal configuration
5. Gradually restore features

## Maintenance

### Daily
- [ ] Check logs for errors
- [ ] Monitor cost usage
- [ ] Review user feedback

### Weekly
- [ ] Analyze cost trends
- [ ] Review performance metrics
- [ ] Check for OpenAI API updates
- [ ] Backup database

### Monthly
- [ ] Update dependencies
- [ ] Review and optimize prompts
- [ ] Analyze usage patterns
- [ ] Plan feature enhancements

## Support

### Common Issues

**Agent won't start**
- Check .NET SDK version
- Verify API key is set
- Check logs for errors

**No responses from agent**
- Verify OpenAI API key is valid
- Check internet connectivity
- Review API rate limits

**Search not working**
- Ensure AiSearch service is running
- Check AiSearch URL in config
- Test AiSearch directly: `curl http://localhost:54321/health`

**High costs**
- Review cost tracking logs
- Check for runaway queries
- Adjust cost limits
- Consider using cheaper models

**Database errors**
- Check file permissions
- Verify disk space
- Review database schema
- Backup and recreate if corrupted

### Getting Help
1. Check FINAL_SUMMARY.md for overview
2. Review USAGE_EXAMPLES.md for code patterns
3. Check IMPLEMENTATION_STATUS.md for known issues
4. Review logs in `logs/agent-*.log`
5. Test with minimal configuration

## Sign-Off

### Deployment Team
- [ ] Technical Lead: _________________ Date: _______
- [ ] QA Lead: _________________ Date: _______
- [ ] Product Owner: _________________ Date: _______

### Approval
- [ ] Ready for Beta Testing
- [ ] Ready for Production
- [ ] Requires Additional Work

### Notes
_______________________________________________________
_______________________________________________________
_______________________________________________________
