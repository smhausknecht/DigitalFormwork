# DigitalFormwork
DigitalFormwork is a Grasshopper plug-in that enables 3D-printable formwork design, offering support for modeling formwork and analyzing its removability and manufacturability.

<p align="center">
  <img src="Icons/Icon_DigitalFormwork_Large.png" alt="DF Logo" width="200"/>
</p>

## Components
- Analysis:
	-	Generate Vector From Mesh Face Normal
	-	Generate Vector From Brep Face Normal
	-	View Removal Vector Coverage On Mesh
	-	View Removal Vector Coverage On Brep
	-	Check Formwork Removal
	-	Check Print Volume
- Modeling:
	-	Offset Brep
	-	Cut Solid Mesh By Planes
	-	Cut Solid Brep By Planes
	-	Cut Solid Mesh By Mesh Surface
	-	Cut Solid Brep By Brep Surface
	-	Create Mesh Shell with Plane
	-	Create Brep Shell with Plane
	-	Create Mesh Shell with Mesh Surface
	-	Create Brep Shell with Brep Surface
- Bugfix:
	-	Planarize Mesh
	-	Prepare Brep
	-	Rebuild Brep

## How to use it
Generate a removal vector that indicates the direction a formwork part will be removed in with either native Grasshopper components or the 'Generate Vector From [...]' components.
You can then analyze the geometry that you want to create formwork for, with the 'View Removal Vector Coverage [...]' components to see whether vectors you are using are sufficient and where they might lead to problems.
Next, create formwork parts. I suggest using DigitalFormworks 'Offset Brep' or the native components 'Bounding Box' or 'Shrink Wrap'. All that matters is that the models used are solid and valid. If they aren't, try using the 'Bugfix' components. Use Rhinos '_MeshRepair' for meshes.
Afterwards you can cut the solids into formwork parts with planes or surfaces - either with the 'Create [...] Shell [..]' components or by using a 'Cut Solid [...]' component and cutting out the cast body with 'Boolean Difference' afterwards.
Don't forget to add a funnel for pouring material in. To ensure that the formwork parts don't fall apart while pouring, add places to place clamps or bolts. Also, prevent them from sliding apart. A simple way to do it is to connect them with a slot-and-key connection using basic shapes (e.g. boxes).
Once you have the formwork parts, check them again with 'Check Formwork Removal' to make sure there are no collisions with the cast body.
If you want to 3D-print your formwork parts, use 'Check Print Volume' to make sure your printer is big enough to print your parts. Export parts as .stl and print.

## Development
DigitalFormwork was built for Rhinoceros 8 and Grasshopper version 8.24.25281.15001 using .NET Framework 4.8 and was entirely written in C#.

You can check out the source code at: https://github.com/smhausknecht/DigitalFormwork

At this stage of development 'View Removal Vector Coverage On Brep' is limited to Breps with planar faces. I will work to expand functionality when I find the time.

## License

GPL-3.0

## Author
Simon M. Hausknecht
