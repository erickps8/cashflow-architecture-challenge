using Asp.Versioning;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CashFlow.Launch.Api.Controllers;
[ApiController][ApiVersion("1.0")][Route("api/v{version:apiVersion}/recurring-entries")][Authorize]
public class RecurringEntriesController:ControllerBase{
 private readonly IRecurringEntryService _service;public RecurringEntriesController(IRecurringEntryService service)=>_service=service;
 [HttpPost]public async Task<IActionResult>Create(CreateRecurringEntryRequest r){var x=await _service.CreateAsync(r.Amount,r.Type,r.Description,r.AccountId,r.CategoryId,r.Frequency,r.StartAt,r.EndAt);return x is null?BadRequest():Ok(x);}
 [HttpPut("{id:guid}")]public async Task<IActionResult>Update(Guid id,UpdateRecurringEntryRequest r){var x=await _service.UpdateAsync(id,r.Amount,r.Type,r.Description,r.AccountId,r.CategoryId,r.Frequency,r.StartAt,r.EndAt,r.IsActive);return x is null?BadRequest():Ok(x);}
 [HttpDelete("{id:guid}")]public async Task<IActionResult>Delete(Guid id)=>await _service.DeleteAsync(id)?NoContent():NotFound();
 [HttpGet]public async Task<IActionResult>GetAll()=>Ok(await _service.GetAllAsync());
 [HttpPost("generate")]public async Task<IActionResult>Generate([FromQuery]DateTime until)=>Ok(new{generated=await _service.GenerateDueEntriesAsync(until)});
}
