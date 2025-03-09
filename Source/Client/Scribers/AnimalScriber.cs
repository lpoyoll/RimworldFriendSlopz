using Shared;
using Verse;

namespace GameClient.Scribers
{
    public static class AnimalScriber
    {
        public static AnimalFile AnimalToString(Pawn animal)
        {
            AnimalFile animalData = new AnimalFile();

            animalData.ID = animal.ThingID;

            animalData.ScribeData = RTScriber.ThingToScribe(animal);

            return animalData;
        }

        public static Pawn StringToAnimal(AnimalFile file)
        {
            return (Pawn)RTScriber.ScribeToThing(file.ScribeData);
        }
    }
}
