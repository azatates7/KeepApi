using KeepApi.Models;
using KeepApi.Services;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace KeepApi.Controllers
{
    /// <summary>
    /// Not/todo kayıtları için CRUD api controller class
    /// http://localhost:5080/swagger/index.html
    /// </summary>
    [ApiController]
    [Route("api/notes")]
    [Produces("application/json")]
    public class NotesController : ControllerBase
    {
        private readonly NoteService _noteService;
        private readonly IConfiguration _configuration;

        public NotesController(NoteService noteService, IConfiguration configuration)
        {
            _noteService = noteService;
            _configuration = configuration;
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

        /// <summary>Yeni bir not oluşturur.</summary>
        /// <param name="note">Oluşturulacak notun içeriği (id/createdAt gönderilse de yok sayılır, sunucu üretir).</param>
        /// <response code="201">Not oluşturuldu, Location header'ı yeni kaydın adresini gösterir.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Note), StatusCodes.Status201Created)]
        public async Task<ActionResult<Note>> Create([FromBody] Note note, CancellationToken cancellationToken = default)
        {
            var created = await _noteService.CreateAsync(note, cancellationToken);
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
        public async Task<ActionResult<Note>> Update(string id, [FromBody] Note note, CancellationToken cancellationToken = default)
        {
            var updated = await _noteService.UpdateAsync(id, note, cancellationToken);
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
        public async Task<IActionResult> TestOracleConnection(CancellationToken cancellationToken = default)
        {
            var conn = new OracleConnection(_configuration["ConnectionStrings:Oracle"]);

            try
            {
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
    }
}
