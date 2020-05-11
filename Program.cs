using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ProductorAPI.Models;
using Newtonsoft.Json.Linq;

namespace ProductorAPI
{
    public class Program
    {
        public static int cantidad_productores, cantidad_productores_activos;
        public static List<Thread> producer_threads = new List<Thread>();
        public static List<List<string>> thread_elements = new List<List<string>>();
        public static Semaphore uso_contador = new Semaphore(1,1);
        public static Semaphore uso_suspend = new Semaphore(1,1);
        public static Semaphore uso_activate = new Semaphore(1,1);
        public static Boolean suspend_bool = false;
        public static Boolean activate_bool = false;
        public static List<Information> threads_information = new List<Information>();
        public static List<string> log = new List<string>();
        public static int operaciones;
        public static List<List<string>> limites;

        public static string url = "url firebase";
        public static string url_cola = "url heroku";
        public static HttpClient cliente = HttpClientFactory.Create();

        public static void Main(string[] args)
        {
            Abecedario abc = new Abecedario();
            //r cliente = HttpClientFactory.Create();
            log.Add("inicio " + fecha() + " Started Main Thread, no producers yet.");
            //cantidad_productores = Int32.Parse(args[0]);
            cantidad_productores = 5;
            cantidad_productores_activos = cantidad_productores;
            limites = abc.get_limits(cantidad_productores);
            //log.Add(fecha() + "Got Producers parameter, total amount: {} .\n...\n...",cantidad_productores_activos.ToString());
            log.Add("exito " + fecha() + " Got producer parameters, total amount: " + cantidad_productores_activos.ToString() + ".");

            log.Add("normal " + fecha() + " Starting Thread Pool.");
 
            //producer threads 
            for(int i = 0; i < cantidad_productores; i++){
                thread_elements.Add(new List<string>());
                producer_threads.Add(new Thread(Producer));
                producer_threads[i].Name = i.ToString();
                producer_threads[i].Start();
                threads_information.Add(new Information {
                    id=producer_threads[i].ManagedThreadId,
                    position=i,
                    status=true,
                    work=0
                });
                log.Add("exito " + fecha() + " Thread " + producer_threads[i].ManagedThreadId + " started producing. ");
            }


            // Aqui comenzamos 
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static async void Producer(){
            int name = Int32.Parse(Thread.CurrentThread.Name);
            List<string> mis_letras = limites[name];


            for(int i = 0; i < mis_letras.Count; i++) {
                await get_elements(mis_letras[i], name);

                JObject json = JObject.Parse(thread_elements[name][i]);
                var value = json[mis_letras[i]];

                foreach (Object elemento in value)
                {
                    bool repetir = true;
                    while (repetir)
                    {
                        Thread.Sleep(1000);
                        // SINCRONIZATION STUFF 
                        uso_suspend.WaitOne();
                        if (suspend_bool)
                        {
                            Console.WriteLine("Me tengo que morir un rato...");
                            log.Add("desactivar " + fecha() + " Suspended Thread " + producer_threads[name].ManagedThreadId + " .");
                            threads_information[name].status = false;
                            cantidad_productores_activos -= 1;
                            suspend_bool = false;
                            uso_suspend.Release();
                            while (true)
                            {
                                Thread.Sleep(500);
                                uso_activate.WaitOne();
                                if (activate_bool)
                                {
                                    activate_bool = false;
                                    Console.WriteLine("Me tengo que revivir un rato...");
                                    log.Add("exito " + fecha() + " Started Thread " + producer_threads[name].ManagedThreadId + " back again.");
                                    threads_information[name].status = true;
                                    cantidad_productores_activos += 1;
                                    uso_activate.Release();
                                    break;
                                }
                                else
                                {
                                    activate_bool = false;
                                    uso_activate.Release();
                                }
                                //log.Add("ando atrapado en el loop." + producer_threads[name].ManagedThreadId);
                            }
                        }
                        else
                        {
                            activate_bool = false;
                            uso_suspend.Release();
                        }

                        
                        //Console.WriteLine(elemento);
                        HttpContent elemento_insertar = new StringContent(elemento.ToString(), Encoding.UTF8, "application/json");
                        var res = await cliente.PostAsync(url_cola + "insertaUno/", elemento_insertar);
                        if (res.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            repetir = false;
                            Console.WriteLine("insertado.");
                            uso_contador.WaitOne();
                            operaciones += 1;
                            uso_contador.Release();
                            threads_information[name].work += 1;
                            log.Add("trabajo " + fecha() + " Thread " + producer_threads[name].ManagedThreadId + " inserted its " + threads_information[name].work + "th item.");
                        }
                        else
                        {
                            Console.WriteLine("error en la cola.");
                            log.Add("error " + fecha() + " Thread " + producer_threads[name].ManagedThreadId + " could not insert in queue.");
                        }
                       
                        //log.Add("trabajo " + fecha() + " Thread " + producer_threads[name].ManagedThreadId + " produced its " + threads_information[name].work + "th item.");

                    }
                }
            }
        }

        public static void Suspend(){
            uso_suspend.WaitOne();
            uso_contador.WaitOne();
            if(cantidad_productores_activos > 0){
                suspend_bool = true;
            }else{
                log.Add("error " + fecha() + " Tried to suspend but all threads are suspended.");
            }
            uso_suspend.Release();
            uso_contador.Release();
        }

        public static void Reactivate(){
            uso_contador.WaitOne();
            if(cantidad_productores_activos < cantidad_productores){
                activate_bool = true;
            }else{
                log.Add("error " + fecha() + " Tried to reactivate but all threads are active.");
            }
            uso_contador.Release();
        }

        public static string fecha(){
            string fecha = DateTime.Now.ToString();
            return fecha;
        }

        public static async Task get_elements(string letra, int id) {
            string valores;
            var res = await cliente.GetAsync(url + letra + ".json");
            if (res != null)
            {
                valores =  await res.Content.ReadAsStringAsync();
                thread_elements[id].Add(valores);
                //Console.WriteLine(thread_elements[id].Count);
            }
            else
            {
                Console.WriteLine("ERR");
                return;
            }
        }
    }

    public class Abecedario {
        string contents = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        List<string> letras;

        public Abecedario() {
            this.letras = new List<string>();
            foreach (char letra in contents) {
                letras.Add(letra.ToString());
            }
        }

        public List<List<string>> get_limits(int total) {
            int separacion = 26 / total;
            int ultimo = 26 - ((total) * separacion);
            List<List<string>> resultado = new List<List<string>>();

            for (int i = 0; i < total; i++) {
                resultado.Add(letras.GetRange((i * separacion),separacion));
            }
            if (ultimo > 0) {
                resultado.Add(letras.GetRange((letras.Count - ultimo), ultimo));
            }
            return resultado;
        }

    }
}
   