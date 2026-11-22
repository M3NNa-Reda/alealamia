using Microsoft.AspNetCore.Http;
namespace alealamia.Application.DTOs.Files
{
    public class UploadFileDto
    {
        public IFormFile File { get; set; } = null!;
        /*
                 بيستخرج المعلومات من الفايل بمجرد ان المستخدم يرفع الفايل م محتاج يكتب اي حاجه عنه
                ده بيستخرج كل حاجه 
                شات الي قالي مليش دعوه انا كنت هكتبهم عادي😂


                null! 
                    عشان تتاكد ان القيمه مش null
         */
    }
}
