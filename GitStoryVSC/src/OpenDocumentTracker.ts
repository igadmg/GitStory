import { Disposable, ExtensionContext, TextDocument, workspace } from 'vscode';

export class OpenDocumentTracker extends Disposable {
    public documents: Array<TextDocument>;

    constructor(context: ExtensionContext) {
        super(() => this.dispose());

        this.documents = new Array<TextDocument>();

        context.subscriptions.push(workspace.onDidOpenTextDocument((td) => {
            this.documents.push(td);
        }));

        context.subscriptions.push(workspace.onDidCloseTextDocument((td) => {
            var i = this.documents.indexOf(td);
            if (i >= 0)
            {
                this.documents.splice(i, 1);
            }
        }));
    }

    dispose() {
    }
}
