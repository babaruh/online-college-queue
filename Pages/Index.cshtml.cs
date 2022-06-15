using System.ComponentModel.DataAnnotations;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using ContentType = MimeKit.ContentType;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace OnlineCollegeQueue.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [BindProperty]
    public EmailModel FormEmail { get; set; }
    
    public void OnGet()
    {
    }

    [HttpPost]
    public async Task<IActionResult> OnPost()
    {
        if (ModelState.IsValid)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration.GetSection("EmailUsername").Value));
            email.To.Add(MailboxAddress.Parse(_configuration.GetSection("EmailUsername").Value));
            email.Subject = FormEmail.FullName;


            var builder = new BodyBuilder();
            foreach (var file in FormEmail.Attachment)
            {
                if (file.Length > 0)
                {
                    byte[] fileBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                }
            }

            builder.HtmlBody = string.Format($@"Phone number:{FormEmail.PhoneNumber}<br>Email:{FormEmail.Email}");
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration.GetSection("EmailUsername").Value, _configuration.GetSection("EmailPassword").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        return Page();
    }
    
}

public class EmailModel
{
    [Required(ErrorMessage = "Field can`t be empty")]
    [StringLength(maximumLength:70, ErrorMessage = "Input Name")]
    [RegularExpression("^[a-zA-Z]*$", ErrorMessage ="Name must contains only alphabet")]
    public string FullName { get; set; }
    
    [Required(ErrorMessage = "Field can`t be empty")]
    public List<IFormFile> Attachment { get; set; }
    
    [Required(ErrorMessage = "Field can`t be empty")]
    [Phone(ErrorMessage = "Add real phone number")]
    public string PhoneNumber { get; set; }
    
    [Required(ErrorMessage = "Add files")]
    [EmailAddress]
    public string Email { get; set; }
}