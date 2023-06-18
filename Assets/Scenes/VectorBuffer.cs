// Class that interfaces CPU and GPU data, where the CPU side is a list
// of some struct T, and the GPU side is a ComputeBuffer.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VectorBuffer<T> where T : struct
{
    public List<T> m_data = new List<T>();
    public int m_allocatedGPUsize = (int)0u;
    public ComputeBuffer m_buffer;

    public T this[int index]
    {
        get { 
            return m_data[index];
        }
        set {

            m_data[index] = value;
        }
    }
    
    // Upload the data from the CPU to the GPU buffer
    public void BufferData(ComputeBufferType bufferType = ComputeBufferType.Default)
    {
      
        if (m_buffer == null || m_buffer.count != m_data.Count)
        {
            if (m_buffer != null)
                m_buffer.Release();

            m_buffer = new ComputeBuffer(m_data.Count, Marshal.SizeOf(typeof(T)), bufferType);
        }

        m_buffer.SetData(m_data);
        m_allocatedGPUsize=Marshal.SizeOf(m_data);
    }

    public List<T> cpu() { return m_data; }

    public ComputeBuffer gpu() { return m_buffer; }

    public int getGPUSize() { return m_allocatedGPUsize; }
    public int getCPUSize() { return Marshal.SizeOf(m_data); }

    public int Length() { return m_data.Count; }
    private void OnDestroy()
    {
        if (m_buffer != null)
            m_buffer.Release();
    }

}
