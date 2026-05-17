using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Common
{
    // Bu class servis metodlarından standart cavab qaytarmaq üçün istifadə olunur.
    // Məqsəd: hər yerdə success, message və data eyni formada gəlsin.
    public class Result<T>
    {
        // Əməliyyat uğurlu olub-olmadığını göstərir.
        public bool IsSuccess { get; set; }

        // İstifadəçiyə və ya UI-a göstəriləcək mesaj.
        public string Message { get; set; } = string.Empty;

        // Əgər əməliyyat data qaytarırsa, həmin data burada saxlanır.
        public T? Data { get; set; }

        // Uğurlu cavab yaratmaq üçün istifadə olunur.
        public static Result<T> Success(T data, string message = "Əməliyyat uğurla tamamlandı.")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        // Uğurlu, amma datasız cavab yaratmaq üçün istifadə olunur.
        public static Result<T> Success(string message = "Əməliyyat uğurla tamamlandı.")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Message = message,
                Data = default
            };
        }

        // Xətalı cavab yaratmaq üçün istifadə olunur.
        public static Result<T> Fail(string message)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default
            };
        }
    }
}
