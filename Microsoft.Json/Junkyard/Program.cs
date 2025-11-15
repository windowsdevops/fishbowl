using System;
using Microsoft.Json.Serialization;

namespace Junkyard
{
    class Program
    {
        static void Main(string[] args)
        {
            var json = @"{""Name"": ""Bart"", ""Age"": 26}";
            var jser = new JsonSerializer(typeof(Person));
            var jres = jser.Deserialize(json);
            var back = jser.Serialize(jres);
            Console.WriteLine(json == back);
        }
    }

    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
