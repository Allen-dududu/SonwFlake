// See https://aka.ms/new-console-template for more information


using SonwFlake;

SnowFlake instance = SnowFlake.GetInstance(null);

for( int i = 0; i < 100; i++)
{
    Console.WriteLine(instance.NextId());
}
