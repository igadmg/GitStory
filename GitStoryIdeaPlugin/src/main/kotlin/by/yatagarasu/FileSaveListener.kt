package by.yatagarasu

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.OSProcessHandler
import com.intellij.openapi.editor.Document
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.project.ProjectManager
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import java.io.File

fun Document.isModified(): Boolean {
    val fdm = FileDocumentManager.getInstance()
    return fdm.isFileModified(fdm.getFile(this)!!)
}

class FileSaveListener : BulkFileListener {
    override fun after(events: MutableList<out VFileEvent>) {
        events.forEach { e ->
            if (e.isFromSave) {
                val fdm = FileDocumentManager.getInstance()
                if (!fdm.unsavedDocuments
                        .fold(false) { c, d ->
                            c || d.isModified() }) {
                    ProjectManager.getInstance().openProjects.forEach { p ->
                        Runtime.getRuntime().exec(arrayOf("git", "story"), null, File(p.basePath))
                    }
                }
            }
        }
    }
}