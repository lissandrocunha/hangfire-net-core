using Hangfire.Server.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hangfire.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {

        #region Variables

        private readonly IWebHostEnvironment _hostingEnvironment;

        #endregion

        #region Constructors

        public JobsController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        #endregion

        #region Methods

        [HttpPost]
        [Route("RestartHangfire")]
        [SwaggerOperation(Summary = "Envia um comando para Hangfire reiniciar o serviço no Windows")]
        [SwaggerResponse(200, "Comando enviado com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> PostExecuteJob()
        {
            int statusCode = JobManager.RestartHangfireServer();

            if (statusCode == StatusCodes.Status200OK)
            {
                return await Task.FromResult(Ok("Comando enviado com sucesso!"));
            }

            return await Task.FromResult(BadRequest(statusCode));
        }

        [HttpGet]
        [Route("Instalados")]
        [SwaggerOperation(Summary = "Exibir os jobs instalados no Hangfire")]
        [SwaggerResponse(200, "lista com os nomes dos jobs instalados no Hanfire", typeof(string[]))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public IEnumerable<string> Get()
        {
            return JobManager.GetInstalledJobs();
        }

        [DisableRequestSizeLimit]
        [HttpPost]
        [Route("UploadJob")]
        [SwaggerOperation(Summary = "Adicionar um Job ao Hangfire")]
        [SwaggerResponse(200, "Job instalado com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> PostUploadJob(IFormFile file)
        {
            if (file == null)
                return await Task.FromResult(BadRequest("Arquivo informado é inválido."));

            if (Path.GetExtension(file.FileName)?.ToLower() != ".zip")
                return await Task.FromResult(BadRequest("O arquivo informado não é do formato zip."));

            string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var jobPath = Path.Combine(applicationFolder, "jobs");
            var jobFolder = Path.Combine(jobPath, Path.GetFileNameWithoutExtension(file.FileName.ToLower()));

            if (Directory.Exists(jobPath)
             && Directory.Exists(jobFolder))
                return await Task.FromResult(BadRequest("O arquivo informado já está instalado."));

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                byte[] bytes = memoryStream.ToArray();

                using (ZipArchive zip = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    Directory.CreateDirectory(Path.Combine(jobPath, Path.GetFileNameWithoutExtension(file.FileName.ToLower())));
                    zip.ExtractToDirectory(Path.Combine(jobPath, Path.GetFileNameWithoutExtension(file.FileName.ToLower())), true);
                }
            }

            Log.Warning("Job " + file.FileName + " adicionado ao hangfire.");
            Log.CloseAndFlush();

            return await Task.FromResult(Ok("Job instalado com sucesso!"));
        }

        [HttpDelete]
        [Route("ExcluirJob")]
        [SwaggerOperation(Summary = "Excluir um Job do Hangfire")]
        [SwaggerResponse(200, "Job <jobName> excluido com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> DeleteJob(string jobName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobName))
                    return await Task.FromResult(BadRequest("Job não informado."));

                var result = JobManager.DeleteJob(jobName);

                if (result != StatusCodes.Status200OK)
                    return await Task.FromResult(new StatusCodeResult(result));

                Serilog.Log.Warning("Job " + jobName + " excluído.");
            }
            catch (Exception ex)
            {
                return await Task.FromResult(BadRequest(ex));
            }

            return await Task.FromResult(Ok("Comando enviado com sucesso!"));
        }

        [HttpPost]
        [Route("ExecuteJob")]
        [SwaggerOperation(Summary = "Executa imediatamente o Job uma única vez")]
        [SwaggerResponse(200, "Job executado com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> PostExecuteJob(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                return await Task.FromResult(BadRequest("Job não informado."));

            JobManager.FireAndForgetJob(jobName.ToLower());

            return await Task.FromResult(Ok("Comando enviado com sucesso!"));
        }

        [HttpPost]
        [Route("AgendarJob")]
        [SwaggerOperation(Summary = "Agendar a execução de um Job")]
        [SwaggerResponse(200, "Job agendado com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> PostScheduleJob(
            [SwaggerParameter("Nome do Job", Required = true)] string jobName,
            [SwaggerParameter("Data de Agendamento (ex: 2020-12-31T23:59)", Required = true)] DateTime dataHora)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                return await Task.FromResult(BadRequest("Job não informado."));

            if (dataHora < DateTime.Now)
                return await Task.FromResult(BadRequest("Data e hora do agendamento é inválido."));

            JobManager.ScheduleJob(jobName, dataHora);

            return await Task.FromResult(Ok("Comando enviado com sucesso!"));
        }

        [HttpPost]
        [Route("ContinuoJob")]
        [SwaggerOperation(Summary = "Adicionar o Job a lista de Jobs com execução contínua")]
        [SwaggerResponse(200, "Job agendado com sucesso!", typeof(string))]
        [SwaggerResponse(500, "Erro interno no servidor")]
        public async Task<IActionResult> PostRecurringJob(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                return await Task.FromResult(BadRequest("Job não informado."));

            JobManager.TaskJob(jobName);

            return await Task.FromResult(Ok("Comando enviado com sucesso!"));
        }

        #endregion

    }

}
