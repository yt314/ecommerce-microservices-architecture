using Microsoft.AspNetCore.Mvc;
using NotificationService.Data;
using NotificationService.DTOs;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationStore _store;

    public NotificationsController(NotificationStore store) => _store = store;

    [HttpPost]
    public async Task<ActionResult<NotificationRecord>> Create(CreateNotificationRequest request)
    {
        var record = await _store.RecordAsync(request, DateTime.UtcNow);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationRecord>>> GetAll()
        => Ok(await _store.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationRecord>> GetById(string id)
    {
        var record = await _store.GetByIdAsync(id);
        return record is null
            ? NotFound(new { error = $"Notification {id} was not found." })
            : Ok(record);
    }
}
