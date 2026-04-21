using Microsoft.AspNetCore.Mvc;

namespace Logbook.Api.Requests;

public class UploadAdifRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    [FromForm(Name = "activationId")]
    public int? ActivationId { get; set; }
}
