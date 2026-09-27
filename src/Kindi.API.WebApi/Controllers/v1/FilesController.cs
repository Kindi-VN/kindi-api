using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class FilesController : ApiControllerBase
{
	private readonly IFileStorage _storage;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public FilesController(IFileStorage storage, IStringLocalizer<SharedResource> localizer)
	{
		_storage = storage;
		_localizer = localizer;
	}

	/// <summary>
	/// Upload file (multipart form-data, field "file"). Trả url tương đối để lưu vào post.
	/// Chỉ user đã đăng nhập mới upload được.
	/// </summary>
	[HttpPost("upload")]
	[Authorize]
	public async Task<IActionResult> Upload(IFormFile file)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest(_localizer["FileEmpty"], new List<string> { _localizer["FileEmpty"] });
		}

		await using var stream = file.OpenReadStream();
		var stored = await _storage.SaveAsync(stream, file.FileName, file.ContentType);

		return Ok(new { url = stored.Url }, _localizer["FileUploadSuccess"]);
	}
}