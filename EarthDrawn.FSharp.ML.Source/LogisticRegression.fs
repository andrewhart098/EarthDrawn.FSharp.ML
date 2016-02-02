﻿namespace EarthDrawn.FSharp.ML.Source

module LogisticRegression =
    open System
    open FSharp.Data
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double

    // ********************************    
    // BUILDING MATRICIES AND READING DATA
    // ********************************
    // Turn the array of string[] to an array of float[]
    let castToFloatList (x : string []): List<float> = 
        x |> Seq.map (fun s -> float s) |> Seq.toList

    // Read in csv and return a Matrix
    let readData (path : string): Matrix<float> = 
        let csv = CsvFile.Load(path)
        let f2d = csv.Rows 
                        |> Seq.map (fun x -> x.Columns) 
                        |> Seq.map (fun x -> castToFloatList x)
                        |> Seq.toList
        matrix f2d

    type LogReg (path: string, a0: float, l0: float, it: int) =
        // Useful variables
        member this.rawData = readData path
        member this.X = Matrix.Build.Dense(this.rawData.RowCount, 1, 1.0)
                                .Append(this.rawData.RemoveColumn(this.rawData.ColumnCount-1))
        member this.y = Matrix.Build.DenseOfColumnVectors(this.rawData.Column(this.rawData.ColumnCount-1))
        member this.alpha = a0
        member this.lambda = l0
        member this.initialTheta = Matrix.Build.Dense(this.X.ColumnCount, 1, 0.0)
        member this.iterations = it
        member this.m = (float this.X.RowCount)
        
        // Perform Logisitc Regression using gradient descent
        member this.gradients = this.gradientDescent 0 this.initialTheta
        member this.costs = this.findCosts this.gradients


        member this.sigmoid z = 1.0 / (1.0 + exp -z)

        // GRADIENT DESCENT

        // Perform ones step of gradient descent 
        // START HERE 2/3       
        member this.descent (theta:Matrix<float>) =
            printfn "%A" theta
            let hx     = (this.X*theta) |> Matrix.map(fun x -> this.sigmoid x)
            let h      = hx-this.y
            let delt_J = (1.0/this.m)*(this.X 
                                        |> Matrix.mapRows (fun i row -> h.[i, 0]*row) 
                                        |> Matrix.sumCols
                                        |> Matrix.Build.DenseOfRowVectors)
            theta - delt_J.Transpose()
            

        // Recursively applies descent function
        member this.gradientDescent (count: int) (gradAccum:Matrix<float>) = 
            if count = 0 then
                this.gradientDescent (count+1) (gradAccum.Append(this.descent this.initialTheta)) 
            elif count <= this.iterations then
                let prevTheta = Matrix.Build.DenseOfColumnVectors(gradAccum.Column(count-1))
                this.gradientDescent (count+1) (gradAccum.Append(this.descent prevTheta))       
            else 
                gradAccum

        // COST FUNCTION

        // Calculate cost associated with weights
        // Not regularized yet
        member this.calculateCost (thetaV:Vector<float>): float = 
            let theta = Matrix.Build.DenseOfColumnVectors(thetaV)
            let h     = (this.X*theta) |> Matrix.map(fun x -> log(this.sigmoid x))
            let h_1   = (this.X*theta) |> Matrix.map(fun x -> log(1.0-(this.sigmoid x))) 
            let term1 = (this.y.*h)
            let term2 = (this.y |> Matrix.map (fun y -> 1.0-y)).*h_1
            let sum   = term1 + term2 |> Matrix.sum
            -1.0/this.m*sum
            //let J     = -1.0/m*sum
            //J + (this.lambda/(2.0*this.m)) * (theta |> Matrix.sum)


        // Given an array of gradients, calculates the cost associated with each gradient
        member this.findCosts (gradients:Matrix<float>) = 
            gradients.EnumerateColumns() 
                        |> Seq.map(fun x -> this.calculateCost x)
                        |> Seq.map (fun x -> [| x |])
                        |> Seq.toArray
//            let 2dCostArray = Array2D.init<float> 2 2 (fun i j -> costArray.[i].[j]) 
//                        
//            Matrix.Build.DenseOfArray c