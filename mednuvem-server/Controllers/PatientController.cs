﻿using Microsoft.AspNetCore.Mvc;
using Server.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using IdSvrHost.Services;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.Extensions;
using server.Services;

namespace Server.Core.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PatientsController : Controller
    {
        private PatientRepository _patientRepository;
        private UserUtilService _userUtilService;

        public PatientsController(PatientRepository patientRepository, UserUtilService userUtilService)
        {
            _patientRepository = patientRepository;
            _userUtilService = userUtilService;
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] Patient viewModel)
        {
            if(viewModel == null || !ModelState.IsValid)
            {
                ModelState.AddModelError("", "Paciente inválido.");
                return BadRequest(ModelState);
            }
            if(Convert.ToDateTime(viewModel.BirthDate) > DateTime.UtcNow)
            {
                ModelState.AddModelError("date", "Data de nascimento inválida.");
            }
            var patient = viewModel;
            patient.Id = Guid.NewGuid().ToString();
            patient.CreatedAt = DateTime.UtcNow;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.TeamId = _userUtilService.GetTeamId(User);

            var result = await _patientRepository.InsertOne(patient);
            if(result.IsError){
                return BadRequest(result);
            }
            return Json(patient.Id);
        }

        [HttpPut("{patientId}")]
        public async Task<IActionResult> Update(string patientId, [FromBody] Patient viewModel)
        {
            if(viewModel == null || !ModelState.IsValid)
            {
                ModelState.AddModelError("", "Paciente inválido.");
                return BadRequest(ModelState);
            }
            if(Convert.ToDateTime(viewModel.BirthDate) > DateTime.UtcNow)
            {
                ModelState.AddModelError("date", "Data de nascimento inválida.");
            }
            var patient = viewModel;
            patient.Id = patientId;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.TeamId = _userUtilService.GetTeamId(User);

            var result = await _patientRepository.UpdateOne(patient);
            if(result.IsError){
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll(int pageSize = 50, int pageNumber = 1, string orderBy = "", string search = "")
        {
            return Ok(await _patientRepository.GetAll(_userUtilService.GetTeamId(User), pageSize, pageNumber, orderBy, search));
        }

        [HttpGet("{patientId}")]
        public async Task<IActionResult> FindOne(string patientId){
            var p = await _patientRepository.FindOne(_userUtilService.GetTeamId(User), patientId);
            if(p == null) {
                return NotFound();
            }
            return Ok(p);
        }

        [HttpDeleteAttribute("{patientId}")]
        public async Task<IActionResult> DeleteOne(string patientId){
            var result = await _patientRepository.DeleteOne(_userUtilService.GetTeamId(User), patientId);
            if(result.IsError){
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IList<IFormFile> files) 
        {
            var formFile = files[0];
            long size = formFile.Length;

            // full path to file in temp location
            //var filePath = Path.GetTempFileName();
            IEnumerable<PatientImportViewModel> records = null;
            if (formFile.Length > 0)
            {
                try {
                    var stream = new MemoryStream();
                    await formFile.CopyToAsync(stream);
                    stream.Position = 0;
                    var textReader = new StreamReader(stream);
                    var csv = new CsvReader( textReader );
                    records = csv.GetRecords<PatientImportViewModel>().ToList().Where(x => x.Name != null);
                } 
                catch(Exception ex)
                {
                    return BadRequest("Erro ao ler o arquivo. " + ex.Message);    
                }

                if(records == null || !records.Any())
                {
                    return BadRequest("Nenhum registro encontrado.");
                }

                try {
                    var patients = records.Select(x => x.ToPatient())
                                          .ToList();
                    await _patientRepository.InsertMany(_userUtilService.GetTeamId(User), patients);
                }
                catch(Exception ex) 
                {
                    return BadRequest("Erro ao salvar registros. " + ex.Message);
                }
            }
            return Ok(new { count = records?.Count() });
        }
        
    }
}
