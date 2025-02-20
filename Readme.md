![Corona DVH Logo](corona.png)
# CoronaConsole

## Overview

This project processes DICOM files to analyze dose-volume histograms (DVH) for various structures in a medical imaging context. The basic process involves projecting 2D polygons from a CT voxel grid onto a corresponding dose voxel grid, creating a supersampled dose grid, rasterizing polygons, resampling the grid, and aggregating dose volumes.

## Prerequisites

- .NET Framework 4.8
- DICOM files for CT, dose, and structure sets

## Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Restore NuGet packages.

## Usage

### Example Code### Algorithm Steps

1. **Project 2D Polygon**: Project 2D polygon from CT voxel grid on a given slice to the closest slice of a corresponding dose voxel grid.
2. **Create Supersampled Grid**: Create a supersampled empty dose grid, where the X and Y voxel directions are split into n parts, and the Z voxel resolution stays fixed with the dose grid.
3. **Rasterize Polygons**: For each polygon/slice combination, fill in the supersampled grid with 1f using scan line rasterization.
4. **Resample Grid**: Resample the supersampled grid back down to the original dose grid voxel size, creating fractional voxel values between 0 and 1f to account for partial voxels.
5. **Aggregate Dose Volumes**: Aggregate the dose volumes by multiplying the full dose voxel size by each partial voxel value in the structure mask, assigning it the dose value of that specific voxel in the dose grid.

## License

This project is licensed under the MIT License.
### Corona DVH - Eclipse-Like Dose Volume Histogram Calculator

## Algorithm Sketch
### Reading DICOM Data
Read CT data into 3D grid
Read structure data into dictionary of structure labels, List of polygonal contours
Read dose data into 3D grid

### Calculating DVH
1. Resample dose grid into CT grid coordinates
2. For each structure, create a signed distance function (SDF) grid in CT grid coordinates
3. Initialize a list of dose volume voxels (double volume, double dose) for each structure - this will represent the discrete DVH
4. Loop through each voxel in the resampled dose grid
	a. If the voxel is inside the structure, add the voxel's dose to the corresponding bin in the DVH for that structure
	b. If the voxel is outside the structure, skip it
	c. For voxels that are on the boundary between inside and outside, subdivide the SDF voxel by a factor of 64 (8x8 in plane)
		i. For each sub-voxel, determine if it is inside or outside the structure >0
		ii. If the sub-voxel is inside, add its dose to the corresponding bin the subvoxel in the DVH for that structure < 0
		iii. If the sub-voxel is outside, skip it

### Other Functions
1. Plot the cumulative DVH using (ScottPlot)
2. Export the DVH to a CSV file (CsvHelper)
3. Add Eclipse like functions GetDoseAtVolume, GetVolumeAtDose, MaxDose, MeanDose, MinDose

### References to Keep in Mind
https://www.researchgate.net/figure/Agreement-acceptance-1-DD-1-DV-between-DVH-calculated-in-SlicerRT-and-DVH_tbl2_314221371
https://www.science.gov/topicpages/d/dose-volume+histogram+dvh.html#:~:text=dose,RESULTS%3A%20We%20found

![Corona Results](corono_results_2_20_25.jpg)