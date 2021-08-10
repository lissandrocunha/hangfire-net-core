using System;
using Xunit;

namespace Hangfire.Job.HelloWorld.Test
{
    public class UnitTest
    {


        #region Variables

        private HelloWorld.BootStrapper _job;

        #endregion

        #region Constructors

        public UnitTest()
        {

        }

        #endregion

        #region Methods

        [Fact(DisplayName = "Serviço executar sem qualquer tipo de Exception")]
        [Trait("Category", "Serviço")]
        public void Test1()
        {
            bool executadoComSucesso = true;

            try
            {
                _job = new HelloWorld.BootStrapper("HelloWorld", Log.LoggerEventLevel.Debug);
                _job.Execute();
            }
            catch (Exception ex)
            {
                executadoComSucesso = (ex == null);
            }

            Assert.True(executadoComSucesso);
        }

        #endregion

    }
}
