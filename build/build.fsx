#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open System
open Fake.Core

type RuntimeId =
    { Version: string
      ShortVersion: string
      OS: string
      Arch: string }

type DockerImageBuild =
    { Name: string
      FileName: string
      Runtime: RuntimeId
      Tag: string
      Registry: string
      BaseImage: DockerImageBuild option
      BuildPath: string
      BuildArgs: (string * string) list
      AdditionalTags: string list
      Testable: bool } with
        member this.FullImageName =
            sprintf "%s/%s:%s%s" this.Registry this.Name this.Runtime.Version this.Tag

        member this.FullFilePath =
            IO.Path.Combine (__SOURCE_DIRECTORY__, "..", "host", this.Runtime.ShortVersion, this.Runtime.OS, this.Runtime.Arch, this.BuildPath, this.FileName)
            |> IO.Path.GetFullPath

        member this.BuildArgsCmd =
            let baseBuildArg =
                match this.BaseImage with
                | Some i -> sprintf "--build-arg BASE_IMAGE=%s" i.FullImageName
                | None -> ""
            this.BuildArgs
            |> Seq.fold
                (fun acc (k, v) ->
                    sprintf "%s --build-arg %s=%s" acc k v)
                baseBuildArg

let currentRuntimeId = { Version = "2.0.10101"; ShortVersion = "2.0"; OS = "stretch"; Arch = "amd64" }

let baseImage =
    { Name = "base"
      FileName = "base.Dockerfile"
      Runtime = currentRuntimeId
      Tag = ""
      Registry = "azure-functions"
      BaseImage = None
      BuildPath = ""
      BuildArgs = []
      AdditionalTags = []
      Testable = false }

let python36Deps = { baseImage with Name = "python"; FileName = "python36-deps"; Tag = "-python3.6-deps"; BuildPath = "python" }
let python36BuildEnv = { python36Deps with FileName = "python36-buildenv.Dockerfile"; Tag = "-python3.6-buildenv"; BuildArgs = [("BASE_PYTHON_IMAGE", python36Deps.FullImageName)]}
let python36Slim = { python36BuildEnv with FileName = "python36-slim.Dockerfile"; Tag = "-python3.6-slim"; BaseImage = Some baseImage; Testable = true }
let python36 = { python36Slim with FileName = "python36.Dockerfile"; Tag = "-python3.6"; BaseImage = Some python36Slim; BuildArgs = [] }
let python36AppService = { python36 with FileName = "python36.Dockerfile"; Tag = "-python3.6-appservice"; BaseImage = Some python36 }

let python37Deps = { baseImage with Name = "python"; FileName = "python37-deps"; Tag = "-python3.7-deps"; BuildPath = "python" }
let python37BuildEnv = { python37Deps with FileName = "python37-buildenv.Dockerfile"; Tag = "-python3.7-buildenv"; BuildArgs = [("BASE_PYTHON_IMAGE", python37Deps.FullImageName)]}
let python37Slim = { python37BuildEnv with FileName = "python37-slim.Dockerfile"; Tag = "-python3.7-slim"; BaseImage = Some baseImage; Testable = true }
let python37 = { python37Slim with FileName = "python37.Dockerfile"; Tag = "-python3.7"; BaseImage = Some python37Slim; BuildArgs = [] }
let python37AppService = { python37 with FileName = "python37.Dockerfile"; Tag = "-python3.7-appservice"; BaseImage = Some python37 }

let python38Deps = { baseImage with Name = "python"; FileName = "python38-deps"; Tag = "-python3.8-deps"; BuildPath = "python" }
let python38BuildEnv = { python38Deps with FileName = "python38-buildenv.Dockerfile"; Tag = "-python3.8-buildenv"; BuildArgs = [("BASE_PYTHON_IMAGE", python38Deps.FullImageName)]}
let python38Slim = { python38BuildEnv with FileName = "python38-slim.Dockerfile"; Tag = "-python3.8-slim"; BaseImage = Some baseImage; Testable = true }
let python38 = { python38Slim with FileName = "python38.Dockerfile"; Tag = "-python3.8"; BaseImage = Some python38Slim; BuildArgs = [] }
let python38AppService = { python38 with FileName = "python38.Dockerfile"; Tag = "-python3.8-appservice"; BaseImage = Some python38 }

let pythonImages = [
    python36Deps; python36BuildEnv; python36Slim; python36; python36AppService;
    python37Deps; python37BuildEnv; python37Slim; python37; python37AppService;
    python38Deps; python38BuildEnv; python38Slim; python38; python38AppService ]

let build (image: DockerImageBuild) =
    printfn "docker build -t '%s' -f %s %s" image.FullImageName image.FullFilePath image.BuildArgsCmd

let test (image: DockerImageBuild) =
    match image.Testable with
    | true -> printfn "npm run test %s --prefix %s" image.FullImageName (IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "test") |> IO.Path.GetFullPath)
    | false -> ()

Target.create "Build" (fun _ ->
    baseImage :: pythonImages
    |> Seq.iter (fun i ->
        build i
        test i
        printf "\n")
    Shell.Exec ("ls", "-al", "/home/ahmed") |> ignore
)

Target.runOrDefault "Build"