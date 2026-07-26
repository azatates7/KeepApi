using KeepApi.Models;
using KeepApi.Services;
using Microsoft.AspNetCore.Mvc;

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

        public NotesController(NoteService noteService)
        {
            _noteService = noteService;
        }

        /// <summary>Tüm notları listeler (aktif + arşivlenmiş).</summary>
        /// <response code="200">Not listesi döner.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<Note>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Note>>> Get()
        {
            return Ok(await _noteService.GetAsync());
        }
        
        /// <summary>Tüm notları listeler (aktif + arşivlenmiş + silinmiş).</summary>
        /// <response code="200">Not listesi döner.</response>
        [HttpGet("getall")]
        [ProducesResponseType(typeof(List<Note>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Note>>> GetAll()
        {
            return Ok(await _noteService.GetAllsync());
        }

        /// <summary>Tek bir notu id'sine göre getirir.</summary>
        /// <param name="id">Notun kimliği.</param>
        /// <response code="200">Not bulundu.</response>
        /// <response code="404">Bu id ile bir not yok.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Note), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Note>> GetById(string id)
        {
            var note = await _noteService.GetByIdAsync(id);
            return note is null ? NotFound() : Ok(note);
        }

        /// <summary>Yeni bir not oluşturur.</summary>
        /// <param name="note">Oluşturulacak notun içeriği (id/createdAt gönderilse de yok sayılır, sunucu üretir).</param>
        /// <response code="201">Not oluşturuldu, Location header'ı yeni kaydın adresini gösterir.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Note), StatusCodes.Status201Created)]
        public async Task<ActionResult<Note>> Create([FromBody] Note note)
        {
            var created = await _noteService.CreateAsync(note);
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
        public async Task<ActionResult<Note>> Update(string id, [FromBody] Note note)
        {
            var updated = await _noteService.UpdateAsync(id, note);
            return updated is null ? NotFound() : Ok(updated);
        }

        /// <summary>Bir notu kalıcı olarak siler.</summary>
        /// <param name="id">Silinecek notun kimliği.</param>
        /// <response code="204">Not silindi.</response>
        /// <response code="404">Bu id ile bir not yok.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _noteService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }  
        
        [HttpGet("trash")]
        public async Task<ActionResult<List<Note>>> GetTrash()
        {
            return Ok(await _noteService.GetDeletedAsync());
        }

        [HttpPut("{id}/restore")]
        public async Task<IActionResult> Restore(string id)
        {
            var result = await _noteService.RestoreAsync(id);

            return result ? NoContent() : NotFound();
        }

        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeleteForever(string id)
        {
            var result = await _noteService.DeleteForeverAsync(id);

            return result ? NoContent() : NotFound();
        }
    }
}
