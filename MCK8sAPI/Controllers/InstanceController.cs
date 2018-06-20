using System;
using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;

namespace MCK8sAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstanceController : ControllerBase
    {
        private const string namespaceName = "tenant";
        private readonly IKubernetes client;
        public InstanceController()
        {
            var config = KubernetesClientConfiguration.InClusterConfig();// BuildConfigFromConfigFile("./config");
            client = new Kubernetes(config);



        }
        // GET api/values
        [HttpGet]
        public ActionResult<IList<Server>> Get()
        {

            var returnList = new List<Server>();

            var namespaceList = client.ListNamespace();
            foreach (var item in namespaceList.Items)
            {
                if (item.Metadata.Name.StartsWith(namespaceName, StringComparison.InvariantCulture))
                {
                    var interested = client.ListNamespacedService(item.Metadata.Name);
                    foreach (var instance in interested.Items)
                    {
                        var end = client.ReadNamespacedServiceStatus(instance.Metadata.Name, item.Metadata.Name, "true");
                        var endpoints = new List<Endpoints>();
                        foreach (var ip in end.Status.LoadBalancer.Ingress)
                        {
                            string rconPort = string.Empty;
                            string minecraftPort = string.Empty;
                            foreach (var port in end.Spec.Ports)
                            {
                                rconPort = port.Name == "rcon" ? port.Port.ToString() : rconPort;
                                minecraftPort = port.Name == "minecraft" ? port.Port.ToString() : minecraftPort;
                            }

                            endpoints.Add(new Endpoints { Minecraft = ip.Ip + ":" + minecraftPort, Rcon = ip.Ip + ":" + rconPort });

                            var s = new Server
                            {

                                Name = instance.Metadata.Name,
                                Endpoints = endpoints
                            };
                            returnList.Add(s);
                        }

                    }

                }
            }
            return returnList;
    
        }

        private bool IsNamespaceExisting()
        {
            var NSlist = client.ListNamespace();
            foreach (var item in NSlist.Items)
            {
                if (item.Metadata.Name == namespaceName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsNamespaceExisting(int suffix)
        {
            var NSlist = client.ListNamespace();
            foreach (var item in NSlist.Items)
            {
                if (item.Metadata.Name == namespaceName + suffix.ToString())
                {
                    return true;
                }
            }
            return false;
            
        }



        private bool AddNamespaceFromYaml(int suffix)
        {
            
            var yamlFileName = "./Yamls/addNamespace" + "-" + suffix.ToString() + ".yaml";
            var namespaceObject = Yaml.LoadFromFileAsync<V1Namespace>(yamlFileName).Result;
            var ns = client.CreateNamespace(namespaceObject);
            if (ns != null)
            {
                return true;
            }
            return false;

        }

        private bool AddPersistentVolumeClaimFromYaml(int suffix)
        {
            var yamlFileName = "./Yamls/azure-premium" + "-" + suffix.ToString() + ".yaml";
            var pvcObject = Yaml.LoadFromFileAsync<V1PersistentVolumeClaim>(yamlFileName).Result;
            var pvc = client.CreateNamespacedPersistentVolumeClaim(pvcObject, namespaceName + "-" + suffix.ToString());
            if (pvc != null)
            {
                return true;
            }
            return false;
        }

        private bool AddDeploymentFromYaml(int suffix)
        {
            var yamlConfigFile = "./Yamls/externalCfg.yaml";
            var yamlConfigObject = Yaml.LoadFromFileAsync<V1ConfigMap>(yamlConfigFile).Result;
            var config = client.CreateNamespacedConfigMap(yamlConfigObject, namespaceName + "-" + suffix.ToString());

            var yamlFileName = "./Yamls/deployments" + "-" + suffix.ToString() + ".yaml";
            var depObject = Yaml.LoadFromFileAsync<V1Deployment>(yamlFileName).Result;
            var dep = client.CreateNamespacedDeployment(depObject, namespaceName + "-" + suffix.ToString());
            if (dep != null)
            {
                return true;
            }
            return false;
        }

        private bool AddServiceFromYaml(int suffix)
        {
            var yamlFileName = "./Yamls/services" + "-" + suffix.ToString() + ".yaml";
            var svcObject = Yaml.LoadFromFileAsync<V1Service>(yamlFileName).Result;
            var svc = client.CreateNamespacedService(svcObject, namespaceName + "-" + suffix.ToString());
            if (svc != null)
            {
                return true;
            }
            return false;
        }

        private bool RemoveNamespace(int suffix)
        {
            var removedStatus = client.DeleteNamespace(new V1DeleteOptions
            {
                ApiVersion = "v1",

                OrphanDependents = false,
                GracePeriodSeconds = 5,
                Kind = "Namespace"

            }, namespaceName + "-" + suffix.ToString());

            return true;
        }
     

       
        // POST api/values
        [HttpPost]
        public bool Post()
        {
            int suffix = GetHighestDeploymentNumber();
            if (suffix != -1)
            {
                return AddNamespace(suffix);
            }
            return false;
        }

        private int GetHighestDeploymentNumber()
        {
            int suffix = -1;
            var listInterestedNamespaces = new List<string>();
            var allNameSpaces = client.ListNamespace();
            foreach (var ns in allNameSpaces.Items)
            {
                if (ns.Metadata.Name.StartsWith(namespaceName, StringComparison.InvariantCulture))
                {
                    listInterestedNamespaces.Add(ns.Metadata.Name);
                }
            }

            var orderedList = listInterestedNamespaces.OrderByDescending(x => x);
            if (!orderedList.Any())
            {
                suffix = 0;
            }
            else
            {
                var lastNameSpace = orderedList.First();
                var suffixer = lastNameSpace.Split("-")[1];
                int s = 0;
                var intSuffix = int.TryParse(suffixer, out s);
                suffix = s;
            }
            return suffix;
        }

        private bool AddNamespace(int suffix)
        {
            var d = suffix + 1;
                var addNameSpace = AddNamespaceFromYaml(d);
                    if (addNameSpace)
                    {
                    var pvc = AddPersistentVolumeClaimFromYaml(d);
                        if (pvc)
                        {
                        var deployment = AddDeploymentFromYaml(d);
                            if (deployment)
                            {
                            var service = AddServiceFromYaml(d);
                                return true;
                            }
                        }
                    }
             return false;
        }
        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public bool Delete(int id)
        {
            return RemoveNamespace(id);
        }
    }
}
