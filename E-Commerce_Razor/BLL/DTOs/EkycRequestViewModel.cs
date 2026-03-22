using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class EkycRequestViewModel
    {
        // Thông tin cá nhân
        [Required(ErrorMessage = "Vui lòng nhập Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số CCCD")]
        public string CccdNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
        public string Address { get; set; }

        // Mảng file Upload - Các bản sau có thể cần lưu Database 
        [Required(ErrorMessage = "Vui lòng tải lên mặt trước CCCD")]
        public IFormFile FrontImage { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên mặt sau CCCD")]
        public IFormFile BackImage { get; set; }
        
        // Ảnh chứa base64 khi chụp từ Camera
        [Required(ErrorMessage = "Vui lòng chụp ảnh khuôn mặt")]
        public string LiveFaceBase64 { get; set; }
    }
}
