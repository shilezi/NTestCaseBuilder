#include "testCaseFromAlternativesGenerator.hpp"

namespace SageSerpent
{
    namespace TestInfrastructure
    {
        TestCaseFromAlternativesGenerator::TestCaseFromAlternativesGenerator(C5::HashSet<ITestCaseGenerator ^> ^componentTestCaseGenerators)
        {
            //throw gcnew System::NotImplementedException("*** Unimplemented stub! ***");
        }

        System::Collections::IEnumerator ^TestCaseFromAlternativesGenerator::CreateIterator(System::UInt32 requestedDegreesOfFreedomForCombinationCoverage)
        {
            //throw gcnew System::NotImplementedException("*** Unimplemented stub! ***");
            return nullptr;
        }

        System::UInt32 TestCaseFromAlternativesGenerator::MaximumDegreesOfFreedom::get()
        {
            return 0U;
        }

        System::Boolean TestCaseFromAlternativesGenerator::IsDead::get()
        {
            return false;
        }
    }
}
