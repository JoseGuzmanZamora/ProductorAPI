using System.Collections.Generic;
using ProductorAPI.Models;

namespace ProductorAPI.Data{

    public interface Irepo
    {
        int GetProductores();

        int GetOperaciones();

        void DesactivarProducer();

        void ActivarProducer();

        IEnumerable<Information> GetThreadsInfo();

        IEnumerable<string> GetLog();
    }
}