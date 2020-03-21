using BingoApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace BingoApi.Controllers
{
    public class BingoController : ApiController
    {
        private const string RUNNING_TEXT = "Running";
        private const string FINISHED_TEXT = "Finished";

        [HttpGet]
        [ActionName("CheckPdf")]
        public IHttpActionResult Getbingo_CheckPdf(Guid id)
        {
            string message = string.Empty;

            try
            {
                string share = ConfigurationManager.AppSettings["2pdf_share"].Trim();

                string statusFilePath = Path.Combine(share, id.ToString(), "status.txt");
                if (!File.Exists(statusFilePath))
                {
                    message = "Status file does not exists!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                string status = File.ReadAllText(statusFilePath);
                if (string.IsNullOrWhiteSpace(status))
                {
                    message = "Status file is empty!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                if (status == RUNNING_TEXT)
                {
                    message = RUNNING_TEXT;
                    return Ok(new
                    {
                        Message = message,
                        Status = "Good"
                    });
                }
                else if (status == FINISHED_TEXT)
                {
                    message = FINISHED_TEXT;
                    return Ok(new
                    {
                        Message = message,
                        Status = "Good"
                    });
                }
                else
                {
                    message = status;
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }
            }
            catch (Exception ex)
            {
                message = "Check PDF ready error: " + ex.Message;
                return Ok(new
                {
                    Message = message,
                    Status = "Bad"
                });
            }
        }

        [HttpPost]
        [ActionName("CreatePdf")]
        public IHttpActionResult Postbingo_CreatePdf([FromBody] RequestItem requestItem)
        {
            string message = string.Empty;

            try
            {
                string exe2pdf = ConfigurationManager.AppSettings["2pdf_exe"].Trim();
                string demo2pdf = ConfigurationManager.AppSettings["2pdf_demo"].Trim();
                string usage2pdf = ConfigurationManager.AppSettings["2pdf_usage"].Trim();
                string share = ConfigurationManager.AppSettings["2pdf_share"].Trim();
                string runner = ConfigurationManager.AppSettings["2pdf_runner"].Trim();

                if (string.IsNullOrWhiteSpace(exe2pdf) || string.IsNullOrWhiteSpace(usage2pdf) || string.IsNullOrWhiteSpace(share))
                {
                    message = "One or more parameters in web.config file are empty!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                if (!File.Exists(exe2pdf))
                {
                    message = "2pdf executable file not found on the server!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                if (!Directory.Exists(share))
                {
                    message = "Global share directory for input and output files does not exists!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                if (!File.Exists(runner))
                {
                    message = "Runner executable file not found on the server!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                try
                {
                    Guid checkGuid = Guid.Parse(requestItem.Id.ToString());
                }
                catch
                {
                    message = "Incorrect guid parameter!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                string inDirectory = Path.Combine(share, requestItem.Id.ToString(), "In");
                string outDirectory = Path.Combine(share, requestItem.Id.ToString(), "Out");

                if (!Directory.Exists(inDirectory))
                {
                    message = "Input directory with user files does not exists";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                if (!Directory.Exists(outDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(outDirectory);
                    }
                    catch
                    {
                        message = "Create output directory for new PDF file error!";
                        return Ok(new
                        {
                            Message = message,
                            Status = "Bad"
                        });
                    }
                }

                if (Directory.GetFiles(inDirectory).Count() < 1)
                {
                    message = "Input directory with user files is empty!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                string pdfFileName = string.Empty;
                try
                {
                    // pdfFileName = new FileInfo(Directory.GetFiles(inDirectory).OrderBy(x => new FileInfo(x).Name).FirstOrDefault()).Name;
                    // pdfFileName = pdfFileName.Replace(pdfFileName.Split('.').Last(), "pdf");

                    pdfFileName = requestItem.Id + ".pdf";
                }
                catch
                {
                    message = "Define file name for new PDF file error!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                File.Delete(Path.Combine(outDirectory, pdfFileName));

                string cmd2pdf = string.Format(usage2pdf, inDirectory + "\\*.*", outDirectory, pdfFileName);

                string statusFilePath = Path.Combine(share, requestItem.Id.ToString(), "status.txt");
                if (File.Exists(statusFilePath))
                {
                    message = "This task already exists! Try check if finished!";
                    return Ok(new
                    {
                        Message = message,
                        Status = "Bad"
                    });
                }

                string batchFilePath = Path.Combine(share, requestItem.Id.ToString(), "batch.bat");
                File.Delete(batchFilePath);

                string args = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"", exe2pdf, cmd2pdf, statusFilePath, demo2pdf, RUNNING_TEXT, FINISHED_TEXT);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("\"{0}\" {1}", runner, args));

                File.WriteAllText(batchFilePath, sb.ToString(), Encoding.GetEncoding(866));

                using (System.Diagnostics.Process p = new System.Diagnostics.Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo.Arguments = " /c " + "\"" + batchFilePath + "\"";
                    p.Start();
                    p.WaitForExit();
                }

                return Ok(new
                {
                    Message = "Start creating PDF: " + pdfFileName,
                    Status = "Good"
                });
            }
            catch (Exception ex)
            {
                message = "Create PDF file error: " + ex.Message;
                return Ok(new
                {
                    Message = message,
                    Status = "Bad"
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}