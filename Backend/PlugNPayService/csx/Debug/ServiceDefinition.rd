<?xml version="1.0" encoding="utf-8"?>
<serviceModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="PlugNPayService" generation="1" functional="0" release="0" Id="7de59f57-8136-41e2-bbd9-df27ea9aa2de" dslVersion="1.2.0.0" xmlns="http://schemas.microsoft.com/dsltools/RDSM">
  <groups>
    <group name="PlugNPayServiceGroup" generation="1" functional="0" release="0">
      <componentports>
        <inPort name="WorkerRole1:AsyncPosPedHub" protocol="tcp">
          <inToChannel>
            <lBChannelMoniker name="/PlugNPayService/PlugNPayServiceGroup/LB:WorkerRole1:AsyncPosPedHub" />
          </inToChannel>
        </inPort>
        <inPort name="WorkerRole1:FiscalPrinterHub" protocol="tcp">
          <inToChannel>
            <lBChannelMoniker name="/PlugNPayService/PlugNPayServiceGroup/LB:WorkerRole1:FiscalPrinterHub" />
          </inToChannel>
        </inPort>
        <inPort name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" protocol="tcp">
          <inToChannel>
            <lBChannelMoniker name="/PlugNPayService/PlugNPayServiceGroup/LB:WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </inToChannel>
        </inPort>
        <inPort name="WorkerRole1:PosController" protocol="http">
          <inToChannel>
            <lBChannelMoniker name="/PlugNPayService/PlugNPayServiceGroup/LB:WorkerRole1:PosController" />
          </inToChannel>
        </inPort>
      </componentports>
      <settings>
        <aCS name="Certificate|WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapCertificate|WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" defaultValue="">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" />
          </maps>
        </aCS>
        <aCS name="WorkerRole1Instances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/PlugNPayService/PlugNPayServiceGroup/MapWorkerRole1Instances" />
          </maps>
        </aCS>
      </settings>
      <channels>
        <lBChannel name="LB:WorkerRole1:AsyncPosPedHub">
          <toPorts>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/AsyncPosPedHub" />
          </toPorts>
        </lBChannel>
        <lBChannel name="LB:WorkerRole1:FiscalPrinterHub">
          <toPorts>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/FiscalPrinterHub" />
          </toPorts>
        </lBChannel>
        <lBChannel name="LB:WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput">
          <toPorts>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </toPorts>
        </lBChannel>
        <lBChannel name="LB:WorkerRole1:PosController">
          <toPorts>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/PosController" />
          </toPorts>
        </lBChannel>
        <sFSwitchChannel name="SW:WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp">
          <toPorts>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
          </toPorts>
        </sFSwitchChannel>
      </channels>
      <maps>
        <map name="MapCertificate|WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" kind="Identity">
          <certificate>
            <certificateMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </certificate>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </setting>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </setting>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </setting>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </setting>
        </map>
        <map name="MapWorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" kind="Identity">
          <setting>
            <aCSMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" />
          </setting>
        </map>
        <map name="MapWorkerRole1Instances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1Instances" />
          </setting>
        </map>
      </maps>
      <components>
        <groupHascomponents>
          <role name="WorkerRole1" generation="1" functional="0" release="0" software="C:\Users\aidasa\Desktop\SkyDrive\Projects\PlugNPay\Backend\PlugNPayService\csx\Debug\roles\WorkerRole1" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaWorkerHost.exe " memIndex="-1" hostingEnvironment="consoleroleadmin" hostingEnvironmentVersion="2">
            <componentports>
              <inPort name="AsyncPosPedHub" protocol="tcp" portRanges="2211" />
              <inPort name="FiscalPrinterHub" protocol="tcp" portRanges="2224" />
              <inPort name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" protocol="tcp" />
              <inPort name="PosController" protocol="http" portRanges="2222" />
              <inPort name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp" portRanges="3389" />
              <outPort name="WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp">
                <outToChannel>
                  <sFSwitchChannelMoniker name="/PlugNPayService/PlugNPayServiceGroup/SW:WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
                </outToChannel>
              </outPort>
            </componentports>
            <settings>
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;WorkerRole1&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;WorkerRole1&quot;&gt;&lt;e name=&quot;AsyncPosPedHub&quot; /&gt;&lt;e name=&quot;FiscalPrinterHub&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput&quot; /&gt;&lt;e name=&quot;PosController&quot; /&gt;&lt;/r&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
            <storedcertificates>
              <storedCertificate name="Stored0Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" certificateStore="My" certificateLocation="System">
                <certificate>
                  <certificateMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
                </certificate>
              </storedCertificate>
            </storedcertificates>
            <certificates>
              <certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
            </certificates>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1Instances" />
            <sCSPolicyUpdateDomainMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1UpgradeDomains" />
            <sCSPolicyFaultDomainMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1FaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
      </components>
      <sCSPolicy>
        <sCSPolicyUpdateDomain name="WorkerRole1UpgradeDomains" defaultPolicy="[5,5,5]" />
        <sCSPolicyFaultDomain name="WorkerRole1FaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyID name="WorkerRole1Instances" defaultPolicy="[1,1,1]" />
      </sCSPolicy>
    </group>
  </groups>
  <implements>
    <implementation Id="05b00358-b7d6-4d6f-977d-d29914058af4" ref="Microsoft.RedDog.Contract\ServiceContract\PlugNPayServiceContract@ServiceDefinition">
      <interfacereferences>
        <interfaceReference Id="ca7b98c6-b603-49b1-809b-6056c52c5624" ref="Microsoft.RedDog.Contract\Interface\WorkerRole1:AsyncPosPedHub@ServiceDefinition">
          <inPort>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1:AsyncPosPedHub" />
          </inPort>
        </interfaceReference>
        <interfaceReference Id="1fcbb909-fca5-4b60-8a23-8a7e048c686f" ref="Microsoft.RedDog.Contract\Interface\WorkerRole1:FiscalPrinterHub@ServiceDefinition">
          <inPort>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1:FiscalPrinterHub" />
          </inPort>
        </interfaceReference>
        <interfaceReference Id="299fcb96-5e1d-4d03-98be-b10e8546acb9" ref="Microsoft.RedDog.Contract\Interface\WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput@ServiceDefinition">
          <inPort>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </inPort>
        </interfaceReference>
        <interfaceReference Id="ff819a80-5d95-441a-b677-ef08225850b5" ref="Microsoft.RedDog.Contract\Interface\WorkerRole1:PosController@ServiceDefinition">
          <inPort>
            <inPortMoniker name="/PlugNPayService/PlugNPayServiceGroup/WorkerRole1:PosController" />
          </inPort>
        </interfaceReference>
      </interfacereferences>
    </implementation>
  </implements>
</serviceModel>