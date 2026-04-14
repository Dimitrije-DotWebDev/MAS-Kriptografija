using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly TrigramModel _model;

        public PredictionController(TrigramModel model)
        {
            _model = model;
        }

        [HttpGet("next")]
        public IActionResult GetNext([FromQuery] string prev)
        {
            if (string.IsNullOrWhiteSpace(prev))
                return BadRequest("prev query param missing");

            var predictions = _model.PredictNext(prev);

            return Ok(new
            {
                input = prev,
                predictions
            });
        }
    }
}
