using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Practica.Models;

namespace Practica.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UploadFileController : ControllerBase
    {
        [HttpPost]
        public Response UploadFile([FromForm] FileModel fileModel)
        {
            Response response = new Response();
            try
            {
                string path = Path.Combine(@"C:\MyImages", fileModel.FileName);
                using (Stream stream = new FileStream(path, FileMode.Create))
                {
                    fileModel.file.CopyTo(stream);
                }
                response.StatusCode = 200;
                response.ErrorMessage = "Image created succesfull";
            }
            catch (Exception ex)
            {

                response.StatusCode = 100;
                response.ErrorMessage = "Image unsuccesfull created";
            }
            return response;
        }
    }
}
