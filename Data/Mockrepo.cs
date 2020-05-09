using System.Collections.Generic;
using ProductorAPI.Models;

namespace ProductorAPI.Data
{

    public class Mockrepo : Irepo
    {
        public void ActivarProducer()
        {
            Program.Reactivate();
        }

        public void DesactivarProducer()
        {
            Program.Suspend();
        }

        public IEnumerable<string> GetLog()
        {
            return Program.log;
        }

        public int GetOperaciones()
        {
            Program.uso_contador.WaitOne();
            int value = Program.operaciones;
            Program.uso_contador.Release();
            return value;
        }

        public int GetProductores()
        {
            int valor =  Program.cantidad_productores_activos;
            return valor;
        }

        public IEnumerable<Information> GetThreadsInfo()
        {
            return Program.threads_information;
        }
    }
}