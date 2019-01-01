using System;

namespace PresetConverter
{
	/// <summary>
	/// Preset interface
	/// </summary>
	public interface Preset
	{

		bool Read(string filePath);
		
		bool Write(string filePath);
		
	}
}
