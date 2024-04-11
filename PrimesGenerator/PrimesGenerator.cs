
namespace PrimesGenerator
{
    class PrimesGenerator
    {
        private decimal _seed;

        public PrimesGenerator(decimal seed)
        {
            _seed = seed;
        }

        public decimal GetNext()
        {
            switch (_seed)
            {
                case 1:
                    _seed = 2;
                    return 2;
                case 2:
                    _seed = 3;
                    return 3;
            }

            for (decimal candidate = _seed + 2; candidate < decimal.MaxValue; candidate += 2)
            {
                var found = true;
                for (decimal test = 3; test * test <= candidate; test += 2)
                {
                    if (candidate % test == 0)
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    _seed = candidate;
                    return candidate;
                }
            }
            throw new Exception("Next prime is too large");
        }

    }
}
