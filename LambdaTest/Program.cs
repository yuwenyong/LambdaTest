using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{

    class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Func<bool> f = () => true;
            Expression<Func<bool>> e = () => true;
            List<Person> l = new List<Person>
            {
                new Person{Id=1, Name="P1"},
                new Person{Id=2, Name="P2"}
            };

            l.Where(p => p.Id == 2);

            Expression<Func<Person, bool>> expression = p => p.Id != 1 || !p.Name.Contains("xyz") && p.Name.StartsWith("yy");

            //ExpressionWriter.Write(Console.Out, expression);

            DBFilterTranslator translator = new DBFilterTranslator();
            var filter = translator.Translate(expression);

            Console.WriteLine("QueryText: {0}", filter.QueryText);

            foreach (var param in filter.Parameters)
            {
                Console.WriteLine("Name: {0}, Value: {1}", param.Name, param.Value.ToString());
            }
        }
    }
}
