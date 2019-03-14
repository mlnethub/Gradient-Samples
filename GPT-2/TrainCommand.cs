﻿namespace Gradient.Samples.GPT2 {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using ManyConsole.CommandLineUtils;
    class TrainCommand: ConsoleCommand {
        public override int Run(string[] remainingArguments) {
            this.CheckRequiredArguments();
            if (remainingArguments.Length < 1)
                throw new ArgumentNullException("dataset");
            string datasetName = remainingArguments[0];
            string checkpoint = this.Checkpoint;
            switch (this.Checkpoint) {
            case "latest":
                checkpoint = Gpt2Trainer.GetLatestCheckpoint(this.ModelName, this.RunName);
                break;
            case "fresh":
                checkpoint = Gpt2Trainer.GetOriginalCheckpoint(this.ModelName);
                break;
            }

            var encoder = Gpt2Encoder.LoadEncoder(this.ModelName);
            string searchPattern = this.Include ?? "*";
            var dataset = Gpt2Trainer.LoadDataset(encoder, path: datasetName, pattern: searchPattern);
            var hParams = Gpt2Model.LoadHParams(this.ModelName);
            var random = this.Seed == null ? new Random() : new Random(this.Seed.Value);
            var stop = new CancellationTokenSource();
            Console.CancelKeyPress += delegate { stop.Cancel(); };
            new Gpt2Trainer(dataset, encoder, hParams, this.BatchSize, this.SampleLength, random)
                .Train(checkpoint, this.RunName, stop.Token);

            return 0;
        }

        public string ModelName { get; set; } = "117M";
        public int? Seed { get; set; }
        public int BatchSize { get; set; } = 1;
        public int SampleLength { get; set; } = 1024;
        public int SampleNum { get; set; } = 1;
        public int SampleEvery { get; set; } = 100;
        public int SaveEvery { get; set; } = 1000;
        public string RunName { get; set; } = DateTime.Now.ToString("s").Replace(':', '-');
        public string Checkpoint { get; set; } = "latest";
        public string Include { get; set; }

        public TrainCommand() {
            this.IsCommand("train");
            this.HasAdditionalArguments(1, "<dataset>");
            this.HasOption("m|model=", "Which model to use", name => this.ModelName = name);
            this.HasOption("s|seed=",
                "Explicitly set seed for random generators to get reproducible results",
                (int s) => this.Seed = s);
            this.HasOption("i|include=", "Pattern of files to include in training",
                pattern => this.Include = pattern);
            this.HasOption("n|sample-num=", "",
                (int count) => this.SampleNum = count);
            this.HasOption("b|batch-size=", "Size of the batch, must divide sample-count",
                (int size) => this.BatchSize = size);
            this.HasOption("l|sample-length=", "",
                (int len) => this.SampleLength = len);
            this.HasOption("sample-every=", "Print a sample every N epochs",
                (int n) => this.SampleEvery = n);
            this.HasOption("save-every=", "How often to save a model, in epochs",
                (int n) => this.SaveEvery = n);
            this.HasOption("r|run=", "Name of the run (to be able to resume)",
                run => this.RunName = run);
            this.HasOption("c|checkpoint=", "Use specific checkpoint to start. Available values: 'latest' (default), 'fresh', or path to a checkpoint file",
                checkpoint => this.Checkpoint = checkpoint);
        }
    }
}
