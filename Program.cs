using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductorAPI.Models;

namespace ProductorAPI
{
    public class Program
    {
        public static int cantidad_productores, cantidad_productores_activos;
        public static List<Thread> producer_threads = new List<Thread>();
        public static Semaphore uso_contador = new Semaphore(1,1);
        public static Semaphore uso_suspend = new Semaphore(1,1);
        public static Semaphore uso_activate = new Semaphore(1,1);
        public static Boolean suspend_bool = false;
        public static Boolean activate_bool = false;
        public static List<Information> threads_information = new List<Information>();
        public static List<string> log = new List<string>();
        public static int operaciones;
        public static void Main(string[] args)
        { 
            log.Add("inicio " + fecha() + " Started Main Thread, no producers yet.");
            cantidad_productores = Int32.Parse(args[0]);
            cantidad_productores_activos = cantidad_productores;
            //log.Add(fecha() + "Got Producers parameter, total amount: {} .\n...\n...",cantidad_productores_activos.ToString());
            log.Add("exito " + fecha() + " Got producer parameters, total amount: " + cantidad_productores_activos.ToString() + ".");

            log.Add("normal " + fecha() + " Starting Thread Pool.");
            // producer threads 
            for(int i = 0; i < cantidad_productores; i++){
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

        public static void Producer(){
            int name = Int32.Parse(Thread.CurrentThread.Name);
            // loop principal 
            while(true){

                // WORK  PRODUCE THINGS PLEASE 
                uso_contador.WaitOne();
                operaciones += 1;
                threads_information[name].work += 1;
                Thread.Sleep(300);
                log.Add("trabajo " + fecha() + " Thread " + producer_threads[name].ManagedThreadId + " produced its " + threads_information[name].work + "th item.");
                uso_contador.Release();

                // SINCRONIZATION STUFF 
                uso_suspend.WaitOne();
                if(suspend_bool){
                    Console.WriteLine("Me tengo que morir un rato...");
                    log.Add("desactivar " + fecha() + " Suspended Thread " + producer_threads[name].ManagedThreadId + " .");
                    threads_information[name].status = false;
                    cantidad_productores_activos -= 1;
                    suspend_bool = false;
                    uso_suspend.Release();
                    while(true){
                        //nada 
                        uso_activate.WaitOne();
                        if(activate_bool){
                            activate_bool = false;
                            Console.WriteLine("Me tengo que revivir un rato...");
                            log.Add("exito " + fecha() + " Started Thread " + producer_threads[name].ManagedThreadId + " back again.");
                            threads_information[name].status = true;
                            cantidad_productores_activos += 1;
                            uso_activate.Release();
                            break;
                        }else{
                            uso_activate.Release();
                        }
                    }
                }else{
                    uso_suspend.Release();
                }
            }
        }

        public static void Suspend(){
            uso_contador.WaitOne();
            if(cantidad_productores_activos > 0){
                suspend_bool = true;
            }else{
                log.Add("error " + fecha() + " Tried to suspend but all threads are suspended.");
            }
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
    }
}
