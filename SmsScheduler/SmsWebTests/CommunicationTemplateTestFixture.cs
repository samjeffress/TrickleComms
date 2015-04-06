using ConfigurationModels;
using NUnit.Framework;

namespace SmsWebTests
{
    [TestFixture]
    public class CommunicationTemplateTestFixture
    {
        [Test]
        public void EmailContainsCurlyBracketUsesVariableName()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.EmailContent = "<stuff>{var1}</stuff><p>{var2}</p>";

            communicationTemplate.ExtractVariables();

            Assert.That(communicationTemplate.TemplateVariables[0].VariableName, Is.EqualTo("var1"));
            Assert.That(communicationTemplate.TemplateVariables[1].VariableName, Is.EqualTo("var2"));
            Assert.That(communicationTemplate.TemplateVariables.Count, Is.EqualTo(2));
        }

        [Test]
        public void SmsContainsCurlyBracketUsesVariableName()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.SmsContent = "stuff{var1}stuff{var2}";

            communicationTemplate.ExtractVariables();

            Assert.That(communicationTemplate.TemplateVariables[0].VariableName, Is.EqualTo("var1"));
            Assert.That(communicationTemplate.TemplateVariables[1].VariableName, Is.EqualTo("var2"));
            Assert.That(communicationTemplate.TemplateVariables.Count, Is.EqualTo(2));
        }

        [Test]
        public void SmsAndEmailContainsCurlyBracketUsesVariableName_DuplicateEntriesRemoved()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.SmsContent = "stuff{var1}stuff{var2}";
            communicationTemplate.EmailContent = "<stuff>{var1}</stuff><p>{var2}</p>";

            communicationTemplate.ExtractVariables();

            Assert.That(communicationTemplate.TemplateVariables[0].VariableName, Is.EqualTo("var1"));
            Assert.That(communicationTemplate.TemplateVariables[1].VariableName, Is.EqualTo("var2"));
            Assert.That(communicationTemplate.TemplateVariables.Count, Is.EqualTo(2));
        }

        [Test]
        public void EmailContainsUnevenNumberOfCurlyBracketsRaisesException()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.EmailContent = "<stuff>{var1}}</stuff><p>{var2}</p>";

            Assert.That(() => communicationTemplate.ExtractVariables(),
                Throws.Exception.With.Message.EqualTo("Odd number of opening and closing brackets in template"));
        }

        [Test]
        public void EmailContainsOverlappingOpeningAndClosingBrackets_RaisesException()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.EmailContent = "<stuff>{var1</stuff><p>{var2}}</p>";

            Assert.That(() => communicationTemplate.ExtractVariables(),
                Throws.Exception.With.Message.EqualTo("Brackets must be open and closed before creating new ones"));
        }

        [Test]
        public void EmailContainsOutOfOrderOpeningAndClosingBrackets_RaisesException()
        {
            var communicationTemplate = new CommunicationTemplate();
            communicationTemplate.EmailContent = "<stuff>}{var1</stuff><p>{var2}</p>";

            Assert.That(() => communicationTemplate.ExtractVariables(),
                Throws.Exception.With.Message.EqualTo("Brackets must be open and closed before creating new ones"));
        }
    }
}
