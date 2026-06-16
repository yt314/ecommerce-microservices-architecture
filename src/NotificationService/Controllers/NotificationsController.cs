using Microsoft.AspNetCore.Mvc;
using NotificationService.Data;
using NotificationService.DTOs;

namespace NotificationService.Controllers;

/// <summary>HTTP endpoints for recording/reading notifications (backed by Redis).</summary>
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationStore _store;

    public NotificationsController(NotificationStore store) => _store = store;

    /// <summary>Record (and "send") a notification.</summary>
    [HttpPost]
    public async Task<ActionResult<NotificationRecord>> Create(CreateNotificationRequest request)
    {
        // DateTime.UtcNow is fine here (not inside a workflow); records need a timestamp.
        var record = await _store.RecordAsync(request, DateTime.UtcNow);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    /// <summary>List all recorded notifications.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationRecord>>> GetAll()
        => Ok(await _store.GetAllAsync());

    /// <summary>Get a single notification by id.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationRecord>> GetById(string id)
    {
        var record = await _store.GetByIdAsync(id);
        return record is null
            ? NotFound(new { error = $"Notification {id} was not found." })
            : Ok(record);
    }
}
