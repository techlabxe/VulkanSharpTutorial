using SharpVulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanSharpTutorial.Common
{
    public class SimpleResourceManager
    {
        public void Regist( VkBuffer buffer, VkDeviceMemory memory )
        {
            m_buffers.Add(buffer);
            m_memories.Add(memory);
        }

        public void Destroy( VkDevice device )
        {
            foreach(var layout in m_pipelineLayouts )
            {
                VulkanAPI.vkDestroyPipelineLayout(device, layout);
            }
            m_pipelineLayouts = null;

            foreach( var layout in m_descLayouts )
            {
                VulkanAPI.vkDestroyDescriptorSetLayout(device, layout);
            }
            if (m_descriptorPool != null)
                VulkanAPI.vkDestroyDescriptorPool(device, m_descriptorPool);
            m_descriptorPool = null;

            foreach( var buffer in m_buffers)
            {
                VulkanAPI.vkDestroyBuffer(device, buffer);
            }
            m_buffers = null;
            foreach( var mem in m_memories )
            {
                VulkanAPI.vkFreeMemory(device, mem);
            }
        }

        public VkDescriptorPool CreateDescriptorPool( VkDevice device, VkDescriptorPoolSize[] descPoolSize, int maxSets )
        {
            var descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
            {
                poolSizes = descPoolSize,
                maxSets = (uint)maxSets,
                flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT,
            };
            VulkanAPI.vkCreateDescriptorPool(device, ref descriptorPoolCreateInfo, out m_descriptorPool);
            return m_descriptorPool;
        }
        public VkDescriptorSetLayout CreateDescriptorSetLayout( VkDevice device, VkDescriptorSetLayoutBinding[] bindings )
        {
            VkDescriptorSetLayout layout = null;
            var descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo();
            descriptorSetLayoutCreateInfo.bindings = bindings;
            VulkanAPI.vkCreateDescriptorSetLayout(device, ref descriptorSetLayoutCreateInfo, out layout);
            m_descLayouts.Add(layout);
            return layout;
        }
        public VkPipelineLayout CreatePipelineLayout( VkDevice device, VkDescriptorSetLayout descSetLayout )
        {
            var createInfo = new VkPipelineLayoutCreateInfo();
            createInfo.setLayouts = new[] { descSetLayout };
            VkPipelineLayout layout = null;
            VulkanAPI.vkCreatePipelineLayout(device, ref createInfo, out layout);
            m_pipelineLayouts.Add(layout);
            return layout;
        }
        private VkDescriptorPool m_descriptorPool;
        private List<VkBuffer> m_buffers = new List<VkBuffer>();
        private List<VkDeviceMemory> m_memories = new List<VkDeviceMemory>();
        private List<VkDescriptorSetLayout> m_descLayouts = new List<VkDescriptorSetLayout>();
        private List<VkPipelineLayout> m_pipelineLayouts = new List<VkPipelineLayout>();
    }
}
