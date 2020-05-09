using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ProductorAPI.Data;
using ProductorAPI.Models;

namespace ProductorAPI.Controllers{

    [Route("prod")]
    [ApiController]
    public class InformationsController : ControllerBase{
        
        private readonly Mockrepo _repositorio = new Mockrepo();

        [HttpGet("numero")]
        public ActionResult <int> GetProdNumber(){
            return Ok(_repositorio.GetProductores());
        }

        [HttpGet("operaciones")]
        public ActionResult <int> GetOpNumber(){
            return Ok(_repositorio.GetOperaciones());
        }

        [HttpGet("desactivar")]
        public ActionResult <int> SuspendProducer(){
            _repositorio.DesactivarProducer();
            return Ok(0);
        }      

        [HttpGet("activar")]
        public ActionResult <int> ReactivateProducer(){
            _repositorio.ActivarProducer();
            return Ok(1);
        }  

        [HttpGet("status")]
        public ActionResult <IEnumerable<Information>> GetProducersStatus(){
            return Ok(_repositorio.GetThreadsInfo());
        }    

        [HttpGet("log")]
        public ActionResult <IEnumerable<Information>> GetProdLog(){
            return Ok(_repositorio.GetLog());
        }    
    }
}