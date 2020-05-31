using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisConsole
{
    class Program
    {
        
        static void Main(string[] args)
        {
            //RedisClient client = new RedisClient("localhost", 6379, "password");
            var redisClient = RedisManager.GetClient();
            
            redisClient.Set<string>("Name", "Kiba5181");
            redisClient.Set<int>("Age", 100001); 
            redisClient.Save();//保存 
            
            Console.WriteLine($"MyName:{redisClient.Get<string>("Name")}====MyAge:{redisClient.Get<int>("Age")}");
            redisClient.Dispose();  //释放内存
            Console.ReadLine();
        } 
    }
}
