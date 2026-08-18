using KeepApi.Data.Entity;
using KeepApi.Models.Request.Note;
using KeepApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Security.Claims;

namespace KeepApi.Controllers
{
    /// <summary>
    /// Not/todo kayıtları için CRUD api controller class
    /// http://localhost:5080/swagger/index.html
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/notes")]
    [Produces("application/json")]
    public class NotesController : ControllerBase
    {
        private readonly NoteService _noteService;
        private readonly IConfiguration _configuration;

        private readonly AttachmentSummaryService _attachmentSummaryService;

        public NotesController(NoteService noteService, IConfiguration configuration, AttachmentSummaryService attachmentSummaryService)
        {
            _noteService = noteService;
            _configuration = configuration;
            _attachmentSummaryService = attachmentSummaryService;
        }

        /// <summary>
        /// Yüklenen bir görsel/belgeyi (resim, PDF, txt) LLM ile özetler ve not olarak kaydedilebilecek bir başlık + içerik döner.
        /// Dosyanın kendisi sunucuda saklanmaz; sadece LLM isteğinde kullanılır, notu oluşturmak için ayrıca normal POST /api/notes çağrılmalıdır.
        /// </summary>
        /// <response code="200">Özet üretildi.</response>
        /// <response code="400">Dosya eksik, çok büyük veya desteklenmeyen bir türde.</response>
        [HttpPost("attachments/summarize")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [ProducesResponseType(typeof(AttachmentSummaryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SummarizeAttachment(IFormFile? file, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Dosya gönderilmedi." });
            }

            if (file.Length > AttachmentSummaryService.MaxAttachmentBytes)
            {
                return BadRequest(new { message = "Dosya çok büyük (maks. 8MB)." });
            }

            if (!AttachmentSummaryService.AllowedMimeTypes.Contains(file.ContentType))
            {
                return BadRequest(new { message = "Desteklenmeyen dosya türü. Görsel, PDF veya metin dosyası yükleyin." });
            }

            using var memoryStrean = new MemoryStream();
            await file.CopyToAsync(memoryStrean, cancellationToken);
            var fileBytes = memoryStrean.ToArray();

            // Content-Type header'ı istemci tarafından belirlenir ve sahtelenebilir;
            // dosyanın gerçek baytları da bildirilen türle uyuşuyor mu kontrol edilir.
            if (!FileSignatureValidator.MatchesClaimedType(fileBytes, file.ContentType))
            {
                return BadRequest(new { message = "Dosya içeriği, bildirilen dosya türüyle uyuşmuyor." });
            }

            try
            {
                var result = await _attachmentSummaryService.SummarizeAsync(
                    fileBytes, file.ContentType, file.FileName, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Özet oluşturulamadı: " + ex.Message });
            }
        }

        /// <summary>Tüm notları listeler (aktif + arşivlenmiş).</summary>
        /// <response code="200">Not listesi döner.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<Note>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Note>>> Get(CancellationToken cancellationToken = default)
        {
            return Ok(await _noteService.GetAsync(cancellationToken));
        }

        /// <summary>Tüm notları listeler (aktif + arşivlenmiş + silinmiş).</summary>
        /// <response code="200">Not listesi döner.</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("getall")]
        [ProducesResponseType(typeof(List<Note>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Note>>> GetAll(CancellationToken cancellationToken = default)
        {
            return Ok(await _noteService.GetAllAsync(cancellationToken));
        }

        /// <summary>Tek bir notu id'sine göre getirir.</summary>
        /// <param name="id">Notun kimliği.</param>
        /// <response code="200">Not bulundu.</response>
        /// <response code="404">Bu id ile bir not yok.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Note), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Note>> GetById(string id, CancellationToken cancellationToken = default)
        {
            var note = await _noteService.GetByIdAsync(id, cancellationToken);
            return note is null ? NotFound() : Ok(note);
        }

        /// <summary>
        /// Arama ekranı için aktif, sabitlenmiş, arşivlenmiş ve çöp kutusundaki tüm kayıtları döndürür. Kalıcı olarak silinenler gösterilmez.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<Note>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Note>>> SearchRecords(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _noteService.GetSearchableAsync(cancellationToken));
        }

        /// <summary>Yeni bir not oluşturur.</summary>
        /// <param name="note">Oluşturulacak notun içeriği (id/createdAt gönderilse de yok sayılır, sunucu üretir).</param>
        /// <response code="201">Not oluşturuldu, Location header'ı yeni kaydın adresini gösterir.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Note), StatusCodes.Status201Created)]
        public async Task<ActionResult<Note>> Create([FromBody] CreateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var created = await _noteService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Var olan bir notu (başlık, içerik, renk, pin, arşiv, hatırlatma) günceller.</summary>
        /// <param name="id">Güncellenecek notun kimliği.</param>
        /// <param name="note">Notun yeni hali.</param>
        /// <response code="200">Not güncellendi.</response>
        /// <response code="404">Bu id ile bir not yok.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Note), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Note>> Update(string id, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var updated = await _noteService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        /// <summary>Bir notu kalıcı olarak siler.</summary>
        /// <param name="id">Silinecek notun kimliği.</param>
        /// <response code="204">Not silindi.</response>
        /// <response code="404">Bu id ile bir not yok.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
        {
            var deleted = await _noteService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("trash")]
        public async Task<ActionResult<List<Note>>> GetTrash(CancellationToken cancellationToken = default)
        {
            return Ok(await _noteService.GetDeletedAsync(cancellationToken));
        }

        [HttpPut("{id}/restore")]
        public async Task<IActionResult> Restore(string id, CancellationToken cancellationToken = default)
        {
            var result = await _noteService.RestoreAsync(id, cancellationToken);

            return result ? NoContent() : NotFound();
        }

        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeleteForever(string id, CancellationToken cancellationToken = default)
        {
            var result = await _noteService.DeleteForeverAsync(id, cancellationToken);

            return result ? NoContent() : NotFound();
        }

        [HttpPost("oracleconnectiontest")]
        [AllowAnonymous]
        public async Task<IActionResult> TestOracleConnection(CancellationToken cancellationToken = default)
        {
            try
            {
                var conn = new OracleConnection(_configuration["ConnectionStrings:OracleConnection"]);
                conn.Open();
                Console.WriteLine("Connected!");
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Ok();
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("Token içinde kullanıcı bilgisi bulunamadı.");
            }

            return userId;
        }
    }
}