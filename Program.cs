// See https://aka.ms/new-console-template for more information


using SonwFlake;

SnowFlake instance = SnowFlake.GetInstance(null);

for (int i = 0; i < 100; i++)
{
    Console.WriteLine(instance.NextId());
}

//var array = new long[1000];
//Parallel.For(0, 1000, i =>
//{
//    var id = SnowFlake.GetInstance(100).NextId();
//    array[i] = id;
//});

//Console.WriteLine(array.Length);
//Console.WriteLine(array.Distinct().Count());
